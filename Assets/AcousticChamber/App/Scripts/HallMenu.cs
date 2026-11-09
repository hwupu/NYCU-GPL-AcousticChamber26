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
    /// from the <see cref="ChamberCatalog"/> (Storybook Screens/Main Hall, PO 2026-11-10). A card is one target: a 16:9 cover
    /// (thumbnail with rounded corners, or the solid <c>cover</c> color) with the C-ID top left and the title bottom left on it,
    /// white at 75 % with a soft shadow; nothing below the cover, no unfinished pill (the launch routing still opens a chamber
    /// with unfinished sessions). Choosing a card fades into the chamber's scene. No Study ID and no DEBUG tag here (Debug mode
    /// lives in each chamber, PO 2026-11-06). The status line shows only when a chamber cannot open.
    /// </summary>
    public class HallMenu : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);
        public const float CoverRadius = 36f;
        /// <summary>Room around the card grid inside the list, so a hovered card (103 %) is not clipped.</summary>
        public const float GridMargin = 8f;
        /// <summary>C-ID and title: 24 u in from the cover edges, white at 75 %, soft shadow (CSS drop-shadow(0 0 4px rgb(0 0 0 / 0.3))).</summary>
        public const float TextInset = 24f, TitleBottom = 12f, TextAlpha = 0.75f, ShadowBlur = 4f, ShadowAlpha = 0.3f, TitleSpacing = -3f;
        public const string Title = "Acoustic Chambers"; // centered (PO 2026-11-10)
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
            UiNavigationBar.CenterTitle(surface.Root);
            _status = surface.AddStatus("");
            _status.transform.parent.gameObject.SetActive(false); // shown only with a message (Open)

            var chambers = catalog != null ? catalog.Visible() : new List<ChamberDefinition>();
            if (chambers.Count == 0)
            {
                var empty = UiKit.Text(surface.Body, "Empty", EmptyText, th.bodySize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.TopLeft);
                UiKit.Size(empty.gameObject, prefW: 0f, flexW: 1f);
                if (catalog == null) Debug.LogWarning("[GPL] HallMenu: no ChamberCatalog set; run Magic > App > Step 3 - Main Hall.");
                return;
            }

            var content = UiKit.List(surface.Body, "Chambers", out var list);
            var grid = new HallGrid(Size.x - 2f * th.surfacePaddingX - 2f * GridMargin, chambers.Count);
            var column = content.GetComponent<VerticalLayoutGroup>();
            column.spacing = grid.RowGap;
            column.padding = UiKit.Pad((int)GridMargin, (int)GridMargin, (int)(UiKit.IndicatorMargin + GridMargin), (int)GridMargin); // the list keeps its indicator margin on the right
            RectTransform row = null;
            for (int i = 0; i < chambers.Count; i++)
            {
                if (i % HallGrid.Columns == 0)
                {
                    row = UiKit.Node(content, "Row " + (i / HallGrid.Columns + 1));
                    UiKit.H(row.gameObject, grid.Gap, null, TextAnchor.UpperLeft);
                    UiKit.Size(row.gameObject, flexW: 1f);
                }
                Card(row, chambers[i], grid);
            }
            if (chambers.Count % HallGrid.Columns == 1) // an empty slot keeps the last card at half width
                UiKit.Size(UiKit.Node(row, "Empty Slot").gameObject, minW: 0f, prefW: 0f, flexW: 1f);
        }

        /// <summary>The Hall's panel (fades out while a chamber loads).</summary>
        public UiPanel Panel { get; private set; }

        void Card(RectTransform row, ChamberDefinition c, HallGrid g)
        {
            var th = UiKit.Theme;
            // the card is only its cover; the button's own fill lies under the cover (hidden), the look is scale + saturation
            var card = UiKit.Node(row, "Card " + c.id);
            var bg = UiKit.Fill(card, Color.clear, CoverRadius);
            bg.raycastTarget = true;
            UiKit.V(card.gameObject, 0f, null, TextAnchor.UpperLeft);
            UiKit.Size(card.gameObject, minW: 0f, prefW: 0f, flexW: 1f); // equal columns whatever the title (min 0)
            var button = card.gameObject.AddComponent<UiButton>();
            button.targetGraphic = bg;
            var nav = button.navigation;
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
            button.Style = UiStyle.Borderless;
            button.onClick.AddListener(() => Open(c));

            // the cover; the C-ID and the title are on it, nothing below it
            var cover = UiKit.Node(card, "Cover");
            var shape = UiKit.Fill(cover, th.cover, CoverRadius); // the rounded "Background" child
            UiKit.Size(cover.gameObject, prefW: 0f, prefH: g.CoverHeight, flexW: 1f);
            RawImage img = null;
            if (c.thumbnail != null)
            {
                // the image is a child of the rounded shape, which masks it (a Mask needs its graphic on the same object)
                shape.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                img = UiKit.Fill(UiKit.Rect(shape.transform, "Thumbnail", Vector2.zero, Vector2.zero)).gameObject.AddComponent<RawImage>();
                img.texture = c.thumbnail;
                img.uvRect = CoverUv(c.thumbnail.width, c.thumbnail.height);
                img.raycastTarget = false;
            }
            HallCard.Attach(button, img); // hover: 103 % (pressed 101.5 %), image saturation 125 %

            var white = new Color(th.text.r, th.text.g, th.text.b, TextAlpha);
            if (!string.IsNullOrEmpty(c.label))
            {
                var id = UiKit.Text(cover.transform, "C-ID", c.label, th.smallSize, Vector2.zero, new Vector2(200f, 40f), bold: true, color: white,
                                    align: TextAlignmentOptions.TopLeft);
                id.textWrappingMode = TextWrappingModes.NoWrap;
                id.rectTransform.anchorMin = id.rectTransform.anchorMax = id.rectTransform.pivot = new Vector2(0f, 1f);
                id.rectTransform.anchoredPosition = new Vector2(TextInset, -TextInset);
                UiKit.SoftShadow(id, ShadowBlur, new Color(0f, 0f, 0f, ShadowAlpha));
            }

            var title = UiKit.Text(cover.transform, "Title", c.CardTitle, th.titleSize, Vector2.zero, Vector2.zero, bold: true, color: white,
                                   align: TextAlignmentOptions.BottomLeft);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            title.characterSpacing = TitleSpacing; // tight, like CSS letter-spacing -0.03em
            var tr = title.rectTransform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = new Vector2(1f, 0f);
            tr.pivot = Vector2.zero;
            tr.offsetMin = new Vector2(TextInset, TitleBottom);
            tr.offsetMax = new Vector2(-TextInset, TitleBottom + th.titleSize * 2f); // room for the whole line (Noto: ~1.45 em); a shorter box makes the ellipsis drop the line
            UiKit.SoftShadow(title, ShadowBlur, new Color(0f, 0f, 0f, ShadowAlpha));
        }

        /// <summary>UV rect that fills the 16:9 cover with an image of any aspect (centered crop, like CSS background-size: cover).</summary>
        public static Rect CoverUv(int width, int height)
        {
            if (width <= 0 || height <= 0) return new Rect(0f, 0f, 1f, 1f);
            float aspect = (float)width / height;
            if (aspect > HallGrid.CoverAspect)
            {
                float w = HallGrid.CoverAspect / aspect;
                return new Rect((1f - w) / 2f, 0f, w, 1f);
            }
            float h = aspect / HallGrid.CoverAspect;
            return new Rect(0f, (1f - h) / 2f, 1f, h);
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
                _status.transform.parent.gameObject.SetActive(true);
                Debug.LogError("[GPL] HallMenu: " + _status.text);
                return;
            }
            Panel.Shown = false;
            if (!AppNav.EnterChamber(c)) Panel.Shown = true;
        }
    }
}
