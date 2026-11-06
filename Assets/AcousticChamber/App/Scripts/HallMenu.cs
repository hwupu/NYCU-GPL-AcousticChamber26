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

        RectTransform _window;
        TextMeshProUGUI _status;

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            var th = UiKit.Theme;
            var canvas = (RectTransform)transform;
            UiKit.Clear(canvas);
            canvas.sizeDelta = Size;
            _window = UiKit.Window(canvas);
            var page = PageStack.On(_window).Push(Title);
            var r = page.Rect;

            float width = Page.ContentWidth(Size.x);
            float top = Size.y / 2f - Page.HeaderHeight - th.gap;
            float bottom = -Size.y / 2f + 72f; // room for the status line
            _status = UiKit.Text(r, "Status", "", th.smallSize, new Vector2(0f, -Size.y / 2f + 36f), new Vector2(width, 40f), color: th.text2);

            var chambers = catalog != null ? catalog.Visible() : new List<ChamberDefinition>();
            if (chambers.Count == 0)
            {
                UiKit.Text(r, "Empty", EmptyText, th.bodySize, new Vector2(0f, (top + bottom) / 2f), new Vector2(width, 200f), color: th.text2);
                if (catalog == null) Debug.LogWarning("[GPL] HallMenu: no ChamberCatalog set; run Magic > App > Step 3 - Main Hall.");
                return;
            }

            var list = ScrollList.Create(r, "Chambers", new Vector2(0f, (top + bottom) / 2f), new Vector2(width, top - bottom));
            var grid = new HallGrid(width, chambers.Count, th.inset);
            for (int i = 0; i < chambers.Count; i++) Card(list.Content, chambers[i], grid, i);
            list.ContentHeight = grid.TotalHeight;
        }

        void Card(RectTransform content, ChamberDefinition c, HallGrid g, int index)
        {
            var th = UiKit.Theme;
            var size = new Vector2(g.CardWidth, g.CardHeight);
            var card = UiKit.Button(content, "Card " + c.id, c.CardTitle, UiStyle.Borderless, Vector2.zero, size, () => Open(c));
            var rt = (RectTransform)card.transform;
            rt.sizeDelta = size;
            UiKit.Round(card.GetComponent<Image>(), CardRadius);
            ScrollList.TopAnchored(rt, g.Top(index), g.X(index));

            // title band below the cover
            var title = UiKit.LabelOf(card);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.fontSize = th.bodySize;
            title.overflowMode = TextOverflowModes.Ellipsis;
            var tr = title.rectTransform;
            tr.anchorMin = new Vector2(0f, 0f);
            tr.anchorMax = new Vector2(1f, 0f);
            tr.offsetMin = new Vector2(UiKit.RowTextPad, 0f);
            tr.offsetMax = new Vector2(-UiKit.RowTextPad, g.TitleBand);

            var cover = UiKit.Box(card.transform, "Cover", new Vector2(0f, g.CoverCenterY), new Vector2(g.CoverWidth, g.CoverHeight), th.cover, CoverRadius);
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
            var layer = _window.GetComponent<UiLayer>();
            if (layer != null) layer.Show(false);
            if (!AppNav.EnterChamber(c) && layer != null) layer.Show(true);
        }
    }
}
