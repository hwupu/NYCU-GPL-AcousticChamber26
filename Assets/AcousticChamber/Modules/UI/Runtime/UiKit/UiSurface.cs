using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// UiSurface (docs/design): the rounded rectangle with the background (radius 32, <c>surface</c> fill) of a panel. It holds a
    /// stack of <see cref="UiPage"/>s: the <see cref="Root"/> page and pages pushed on it (<see cref="Push"/>); pages change in
    /// place (the new one fades in from <c>pageScale</c> while the old one fades out; no slide). The slots of the root page
    /// (<see cref="NavigationBar"/>, <see cref="Body"/>, <see cref="Status"/>, <see cref="Toolbar"/>) are also reachable here.
    /// </summary>
    public class UiSurface : MonoBehaviour
    {
        readonly List<UiPage> _pages = new List<UiPage>();

        /// <summary>The first page.</summary>
        public UiPage Root { get; private set; }

        /// <summary>What ‹ does on the root page (null = the root page has no back button). Set before the root page's bar is built.</summary>
        public Action RootBack;

        /// <summary>Raised after every push and pop with the new top page.</summary>
        public event Action<UiPage> Changed;

        public int Count => _pages.Count;
        public UiPage Top => _pages.Count > 0 ? _pages[_pages.Count - 1] : null;

        public RectTransform NavigationBar => Root.NavigationBar;
        public RectTransform Body => Root.Body;
        public RectTransform Status => Root.Status;
        public RectTransform Toolbar => Root.Toolbar;
        public RectTransform AddNavigationBar() => Root.AddNavigationBar();
        public TextMeshProUGUI AddStatus(string text) => Root.AddStatus(text);
        public RectTransform AddToolbar() => Root.AddToolbar();
        public void Finish() => Root.Finish();

        public static UiSurface Create(Transform parent, Vector2 size)
        {
            var th = UiKit.Theme;
            var rt = UiKit.Rect(parent, "Surface", Vector2.zero, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = th.surface;
            img.raycastTarget = false;
            UiKit.Round(img, th.panelRadius);
            var s = rt.gameObject.AddComponent<UiSurface>();
            s.Root = UiPage.Create(s, "Page", null, first: true);
            s._pages.Add(s.Root);
            return s;
        }

        /// <summary>
        /// Adds a page titled <paramref name="title"/> on top and shows it, with its navigation bar (‹ back, title, optional DEBUG tag
        /// at the right). The page below fades out and stays alive (inactive), so it keeps its state.
        /// </summary>
        public UiPage Push(string title, bool debugTag = false)
        {
            var page = UiPage.Create(this, "Page " + title, title, first: false);
            UiNavigationBar.Create(page, title, UiNavigationBar.Leading.Back, debugTag, Back);
            page.Finish();
            var old = Top;
            _pages.Add(page);
            old.Fade(false, () => { if (old != null && old != Top) old.gameObject.SetActive(false); });
            page.Fade(true, null);
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
            var now = Top;
            now.gameObject.SetActive(true);
            now.Fade(true, null);
            leaving.Fade(false, () => Kill(leaving));
            Changed?.Invoke(now);
            now.RaiseReturned();
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

        static void Kill(UiPage p)
        {
            if (p == null) return;
            if (Application.isPlaying) Destroy(p.gameObject);
            else DestroyImmediate(p.gameObject);
        }
    }

    /// <summary>
    /// One page of a <see cref="UiSurface"/>: a vertical stack of slots filling the surface: <see cref="NavigationBar"/>
    /// (optional), <see cref="Body"/> (fills the rest), <see cref="Status"/> and <see cref="Toolbar"/> (optional). The body keeps 48 u at
    /// the sides; with neither status line nor toolbar it keeps 40 u at the bottom (call <see cref="Finish"/> after adding the slots).
    /// </summary>
    public class UiPage : MonoBehaviour
    {
        public RectTransform NavigationBar { get; private set; }
        public RectTransform Body { get; private set; }
        public RectTransform Status { get; private set; }
        public RectTransform Toolbar { get; private set; }
        public UiSurface Surface { get; private set; }
        public string Title { get; private set; }

        /// <summary>The ‹ button of the navigation bar, or null.</summary>
        public UiButton BackButton { get; internal set; }

        /// <summary>Raised each time the page becomes the top page again (after a page above it was popped).</summary>
        public event Action Returned;

        internal void RaiseReturned() => Returned?.Invoke();

        CanvasGroup _group;
        float _from = 1f, _to = 1f, _t0, _dur;
        Action _done;

        internal static UiPage Create(UiSurface surface, string name, string title, bool first)
        {
            var th = UiKit.Theme;
            var rt = UiKit.Fill(UiKit.Rect(surface.transform, name, Vector2.zero, Vector2.zero));
            UiKit.V(rt.gameObject, 0f, null, TextAnchor.UpperLeft);
            var page = rt.gameObject.AddComponent<UiPage>();
            page.Surface = surface;
            page.Title = title;
            page._group = rt.gameObject.AddComponent<CanvasGroup>();
            page.Body = UiKit.Node(rt, "Body");
            UiKit.V(page.Body.gameObject, 0f, UiKit.Pad(th.surfacePaddingX, 0, th.surfacePaddingX, 0), TextAnchor.UpperLeft);
            UiKit.Size(page.Body.gameObject, flexW: 1f, flexH: 1f);
            page.Finish();
            if (!first) page.Apply(0f);
            return page;
        }

        /// <summary>The navigation bar slot, above the body (created once).</summary>
        public RectTransform AddNavigationBar()
        {
            if (NavigationBar != null) return NavigationBar;
            NavigationBar = UiKit.Node(transform, "Navigation Bar");
            NavigationBar.SetSiblingIndex(0);
            Finish();
            return NavigationBar;
        }

        /// <summary>The status line slot (one small text2 line) below the body.</summary>
        public TextMeshProUGUI AddStatus(string text)
        {
            var th = UiKit.Theme;
            Status = UiKit.Node(transform, "Status");
            UiKit.V(Status.gameObject, 0f, UiKit.Pad(th.surfacePaddingX, 12, th.surfacePaddingX, 28), TextAnchor.MiddleLeft);
            UiKit.Size(Status.gameObject, prefW: 0f, flexW: 1f);
            var t = UiKit.Text(Status, "Label", text, th.smallSize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.Left);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Ellipsis;
            Finish();
            return t;
        }

        /// <summary>The bottom toolbar slot: an 80 u row, 48 u at the sides, 40 u from the bottom edge (mirrors the navigation bar).</summary>
        public RectTransform AddToolbar()
        {
            var th = UiKit.Theme;
            Toolbar = UiKit.Node(transform, "Toolbar");
            UiKit.H(Toolbar.gameObject, th.gap, UiKit.Pad(th.surfacePaddingX, 0, th.surfacePaddingX, th.surfacePaddingBottom), TextAnchor.MiddleLeft);
            // flexW 1: the slot spans the surface (a slot narrower than its content would sit at the left and not center or right-align its children)
            UiKit.Size(Toolbar.gameObject, prefW: 0f, prefH: th.buttonHeight + th.surfacePaddingBottom, flexW: 1f, flexH: 0f);
            Finish();
            return Toolbar;
        }

        /// <summary>Re-applies the paddings that depend on which slots exist (body top after a bar, bottom without status / toolbar).</summary>
        public void Finish()
        {
            var th = UiKit.Theme;
            var g = Body.GetComponent<VerticalLayoutGroup>();
            g.padding = UiKit.Pad(th.surfacePaddingX, NavigationBar != null ? 12f : 0f, th.surfacePaddingX,
                                  Status == null && Toolbar == null ? th.surfacePaddingBottom : 0f);
        }

        /// <summary>
        /// A header action at the right of the navigation bar (Revert, Delete all): a small borderless button (red text for
        /// <see cref="UiStyle.DestructiveText"/>), as wide as its label.
        /// </summary>
        public UiButton AddHeaderAction(string label, UiStyle style, Action onClick)
        {
            var b = UiKit.Button(NavigationBar, "Action " + label, label, style, small: true, minWidth: false, onClick: onClick);
            return b;
        }

        // ---- page transition -----------------------------------------------------------------------------------

        internal void Fade(bool on, Action done)
        {
            var th = UiKit.Theme;
            _from = _group.alpha;
            _to = on ? 1f : 0f;
            _t0 = Time.unscaledTime;
            _dur = th.Duration(th.page);
            _done = done;
            _group.blocksRaycasts = on;
            if (_dur <= 0f || !Application.isPlaying) Complete(_to);
            else Apply(_from);
        }

        void LateUpdate()
        {
            if (_done == null && Mathf.Approximately(_group.alpha, _to)) return;
            float p = Motion.Progress(Time.unscaledTime - _t0, _dur);
            float v = Mathf.LerpUnclamped(_from, _to, p);
            if (Time.unscaledTime - _t0 >= _dur) Complete(_to);
            else Apply(v);
        }

        void Complete(float v)
        {
            Apply(v);
            var done = _done;
            _done = null;
            done?.Invoke();
        }

        void Apply(float alpha)
        {
            _group.alpha = alpha;
            float s = Mathf.Lerp(UiKit.Theme.pageScale, 1f, alpha);
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
