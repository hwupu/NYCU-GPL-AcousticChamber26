using System;
using System.Collections.Generic;
using System.IO;
using GPL.Session;
using GPL.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AcousticChamber.App
{
    /// <summary>
    /// The Main Hall (Architecture section 5, DesignSystem section 7): a 2-column grid of chamber cards, built at run time
    /// from the <see cref="ChamberCatalog"/>. A card is one target: a 16:9 cover (thumbnail or the solid <c>cover</c> color)
    /// with the C-ID and an "N unfinished" pill, the title below. Choosing a card fades into the chamber's scene. No Study ID
    /// and no DEBUG tag here (Debug mode lives in each chamber, PO 2026-11-06).
    /// </summary>
    public class HallMenu : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);
        public const float CardRadius = 40f, CoverRadius = 36f;
        public const string Title = "Main Hall";
        public const string EmptyText = "No chambers yet. Run Magic > App > Sync Chambers.";

        public ChamberCatalog catalog;

        TextMeshProUGUI _status;

        /// <summary>The launch routing ran in this app run (the Hall routes only when the app starts, not on Back to the Hall).</summary>
        static bool _routed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _routed = false;

        void Start()
        {
            Build();
            if (_routed) return;
            _routed = true;
            RouteAtLaunch();
        }

        /// <summary>PB-06b: the last chamber with unfinished sessions opens at its chamber menu (<see cref="SessionRouter"/>).</summary>
        void RouteAtLaunch()
        {
            if (catalog == null) return;
            var visible = catalog.Visible();
            var ids = new List<string>();
            foreach (var c in visible) ids.Add(c.id);
            string last;
            try { last = AppState.Load(AppNav.StatePath).LastChamber; }
            catch (Exception e)
            {
                Debug.LogWarning("[GPL] HallMenu: cannot read " + AppState.FileName + ": " + e.Message);
                return;
            }
            string id = SessionRouter.Route(last, ids, cid => Unfinished(catalog.Find(cid)));
            if (id == null) return;
            Debug.Log("[GPL] HallMenu: launch routing to " + id + " (unfinished sessions)");
            AppNav.RoutedAtLaunch = true;
            Open(catalog.Find(id));
        }

        public void Build()
        {
            UiStrings.EnsureRegistered();
            var th = UiKit.Theme;
            var canvas = (RectTransform)transform;
            UiKit.Clear(canvas);
            canvas.sizeDelta = Size;
            Panel = UiPanel.Create(canvas, "Hall", Size);
            var surface = Panel.Surface;
            UiNavigationBar.Create(surface, Title, UiNavigationBar.Leading.None);
            _status = surface.AddStatus("");

            var chambers = catalog != null ? catalog.Visible() : new List<ChamberDefinition>();
            if (chambers.Count == 0)
            {
                var empty = UiKit.Text(surface.Body, "Empty", EmptyText, th.bodySize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.TopLeft);
                UiLay.Size(empty.gameObject, prefW: 0f, flexW: 1f);
                if (catalog == null) Debug.LogWarning("[GPL] HallMenu: no ChamberCatalog set; run Magic > App > Step 3 - Main Hall.");
                return;
            }

            var content = UiLay.List(surface.Body, "Chambers", out var list);
            content.GetComponent<VerticalLayoutGroup>().spacing = 8f;
            var grid = new HallGrid(Size.x - 2f * th.surfacePaddingX, chambers.Count, th.inset);
            RectTransform row = null;
            for (int i = 0; i < chambers.Count; i++)
            {
                if (i % HallGrid.Columns == 0)
                {
                    row = UiLay.Node(content, "Row " + (i / HallGrid.Columns + 1));
                    UiLay.H(row.gameObject, grid.Gap, null, TextAnchor.UpperLeft);
                    UiLay.Size(row.gameObject, flexW: 1f);
                }
                Card(row, chambers[i], grid);
            }
            if (chambers.Count % HallGrid.Columns == 1) // an empty slot keeps the last card at half width
                UiLay.Size(UiLay.Node(row, "Empty Slot").gameObject, minW: 0f, prefW: 0f, flexW: 1f);
        }

        /// <summary>The Hall's panel (fades out while a chamber loads).</summary>
        public UiPanel Panel { get; private set; }

        void Card(RectTransform row, ChamberDefinition c, HallGrid g)
        {
            var th = UiKit.Theme;
            var card = UiLay.Node(row, "Card " + c.id);
            var bg = UiLay.Fill(card, Color.clear, CardRadius);
            bg.raycastTarget = true;
            UiLay.V(card.gameObject, 0f, UiLay.Pad(g.Inset, g.Inset, g.Inset, 0), TextAnchor.UpperLeft);
            UiLay.Size(card.gameObject, minW: 0f, prefW: 0f, flexW: 1f); // equal columns whatever the title (min 0)
            var button = card.gameObject.AddComponent<UiButton>();
            button.targetGraphic = bg;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.Style = UiStyle.Borderless;
            button.onClick.AddListener(() => Open(c));

            // title band below the cover
            var cover = UiLay.Node(card, "Cover");
            UiLay.Fill(cover, th.cover, CoverRadius);
            UiLay.Size(cover.gameObject, prefW: 0f, prefH: g.CoverHeight, flexW: 1f);
            var title = UiLay.Label(card, "Title", c.CardTitle, th.bodySize, th.text);
            title.overflowMode = TextOverflowModes.Ellipsis;
            var box = (RectTransform)title.transform.parent;
            box.GetComponent<VerticalLayoutGroup>().padding = UiLay.Pad(UiKit.RowTextPad - g.Inset, 0, UiKit.RowTextPad - g.Inset, 4);
            UiLay.Size(box.gameObject, prefW: 0f, prefH: g.TitleBand, flexW: 1f);
            if (c.thumbnail != null)
            {
                var img = UiKit.Fill(UiKit.Rect(cover.transform, "Thumbnail", Vector2.zero, Vector2.zero)).gameObject.AddComponent<RawImage>();
                img.texture = c.thumbnail;
                img.raycastTarget = false;
            }

            if (!string.IsNullOrEmpty(c.label))
            {
                var id = UiKit.Text(cover.transform, "C-ID", c.label, th.smallSize, Vector2.zero, new Vector2(200f, 44f), bold: true,
                                    align: TextAlignmentOptions.MidlineLeft);
                Corner(id.rectTransform, left: true);
            }

            int unfinished = Unfinished(c);
            if (unfinished > 0)
            {
                string s = unfinished + " unfinished";
                var pill = UiKit.Box(cover.transform, "Unfinished", Vector2.zero, new Vector2(10f, 44f), th.prominent.normal, 22f);
                var t = UiKit.Text(pill.transform, "Label", s, th.minimumSize, Vector2.zero, Vector2.zero, bold: true, color: th.prominent.label);
                t.textWrappingMode = TextWrappingModes.NoWrap;
                UiKit.Fill(t.rectTransform);
                pill.rectTransform.sizeDelta = new Vector2(t.GetPreferredValues(s).x + 36f, 44f);
                Corner(pill.rectTransform, left: false);
            }
        }

        /// <summary>Pins <paramref name="rt"/> to the cover's top left or top right corner, 20 u in.</summary>
        static void Corner(RectTransform rt, bool left)
        {
            float x = left ? 0f : 1f;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(x, 1f);
            rt.anchoredPosition = new Vector2(left ? 24f : -20f, -20f);
        }

        /// <summary>Unfinished (resumable) sessions of the chamber; 0 if its data folder cannot be read.</summary>
        static int Unfinished(ChamberDefinition c)
        {
            try { return new SessionStore(Path.Combine(Application.persistentDataPath, c.id)).ListUnfinished().Count; }
            catch (Exception e)
            {
                Debug.LogWarning("[GPL] HallMenu: cannot read the sessions of " + c.id + ": " + e.Message);
                return 0;
            }
        }

        void Open(ChamberDefinition c)
        {
            if (AppNav.Busy) return;
            string problem = AppNav.Problem(c.scenePath);
            if (problem != null)
            {
                _status.text = c.MenuTitle + ": " + problem;
                Debug.LogError("[GPL] HallMenu: " + _status.text);
                return;
            }
            Panel.Shown = false;
            if (!AppNav.EnterChamber(c)) Panel.Shown = true;
        }
    }
}
