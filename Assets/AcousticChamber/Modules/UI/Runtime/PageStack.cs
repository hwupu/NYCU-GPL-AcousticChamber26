using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// A stack of <see cref="Page"/>s in one container (usually the window, <see cref="UiKit.Window"/>). Pages change in
    /// place (DesignSystem section 6): the new page fades in from <c>pageScale</c> while the old one fades out; no slide.
    /// ‹ on a pushed page goes back one page; ‹ on the root page runs <see cref="RootBack"/> (e.g. back to the Hall) and
    /// is only shown when that is set. Pages below the top stay alive (inactive), so they keep their state.
    /// </summary>
    public class PageStack : MonoBehaviour
    {
        readonly List<Page> _pages = new List<Page>();

        /// <summary>‹ on the root page (null = the root page has no back button). Set before the first <see cref="Push"/>.</summary>
        public Action RootBack;

        /// <summary>Raised after every push and pop with the new top page.</summary>
        public event Action<Page> Changed;

        public int Count => _pages.Count;
        public Page Top => _pages.Count > 0 ? _pages[_pages.Count - 1] : null;

        /// <summary>The page stack of <paramref name="container"/> (added if missing).</summary>
        public static PageStack On(RectTransform container)
        {
            var s = container.GetComponent<PageStack>();
            return s != null ? s : container.gameObject.AddComponent<PageStack>();
        }

        /// <summary>
        /// Adds a page titled <paramref name="title"/> on top and shows it (the first page appears without a transition).
        /// <paramref name="debugTag"/>: show the DEBUG tag at the right of the header (Hall and chamber menu in Debug mode).
        /// </summary>
        public Page Push(string title, bool debugTag = false)
        {
            var th = UiKit.Theme;
            var container = (RectTransform)transform;
            var rt = UiKit.Fill(UiKit.Rect(container, "Page " + title, Vector2.zero, Vector2.zero));
            var page = rt.gameObject.AddComponent<Page>();
            page.Stack = this;
            page.Title = title;
            bool first = _pages.Count == 0;
            page.Layer = UiLayer.AttachPage(rt, shown: first);

            // header: ‹ / title / DEBUG
            var size = container.rect.size;
            float d = th.iconButtonSize;
            float y = size.y / 2f - Page.TopPad - d / 2f;
            float left = -size.x / 2f + Page.EdgePad;
            if (!first || RootBack != null)
            {
                page.BackButton = UiKit.IconButton(rt, "Back", UiKit.BackGlyph, new Vector2(left + d / 2f, y), Back);
                left += d + UiKit.IconTitleGap;
            }
            float right = size.x / 2f - Page.EdgePad - (debugTag ? 200f : 0f);
            float w = Mathf.Max(0f, right - left);
            page.TitleText = UiKit.Text(rt, "Title", title, th.titleSize, new Vector2(left + w / 2f, y), new Vector2(w, d),
                                        bold: true, align: TextAlignmentOptions.Left);
            UiKit.SingleLine(page.TitleText); // the 80 u header is shorter than one 56 u line (81 u): TMP would hide it
            page.TitleText.rectTransform.anchoredPosition += new Vector2(0f, th.titleSize * UiKit.OpticalLift);
            if (debugTag) UiKit.DebugTag(rt);

            if (!first)
            {
                var old = Top;
                old.Layer.Show(false, () => { if (old != null) old.gameObject.SetActive(false); });
                page.Layer.Show(true);
            }
            _pages.Add(page);
            Changed?.Invoke(page);
            return page;
        }

        /// <summary>What ‹ does: one page back, or <see cref="RootBack"/> on the root page.</summary>
        public void Back()
        {
            if (_pages.Count > 1) Pop();
            else RootBack?.Invoke();
        }

        /// <summary>Removes the top page; the page below comes back in place. Ignored on the root page.</summary>
        public void Pop()
        {
            if (_pages.Count < 2) return;
            var leaving = Top;
            _pages.RemoveAt(_pages.Count - 1);
            Reveal(Top);
            leaving.Layer.Show(false, () => Kill(leaving));
            Changed?.Invoke(Top);
            Top.RaiseReturned();
        }

        /// <summary>Back to the root page in one transition (pages in between are removed at once).</summary>
        public void PopToRoot()
        {
            while (_pages.Count > 2)
            {
                var p = _pages[_pages.Count - 2];
                _pages.RemoveAt(_pages.Count - 2);
                Kill(p);
            }
            Pop();
        }

        static void Reveal(Page p)
        {
            p.gameObject.SetActive(true);
            p.Layer.Show(true);
        }

        static void Kill(Page p)
        {
            if (p == null) return;
            if (Application.isPlaying) Destroy(p.gameObject);
            else DestroyImmediate(p.gameObject);
        }
    }
}
