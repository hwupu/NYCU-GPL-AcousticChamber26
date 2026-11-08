using System;
using UnityEngine;
using UnityEngine.UI;

namespace GPL.UI
{
    /// <summary>
    /// UiSheet (docs/design): a modal panel opened from another panel (an action sheet, a form): 820 u wide, a navigation bar with
    /// × and the title, and a body for the content. It appears in place on top of the canvas's panels (they are pushed back) and takes
    /// the ray. The height follows the content: call <see cref="Show"/> after filling <see cref="Body"/>.
    /// </summary>
    public class UiSheet
    {
        public UiPanel Panel { get; private set; }
        public UiPage Page => Panel.Surface.Root;
        public RectTransform Body => Page.Body;
        public Vector2 Size { get; private set; }

        /// <summary>Raised after the sheet was closed with ×, before <paramref name="onClose"/>.</summary>
        public event Action Closed;

        public static UiSheet Create(Transform canvas, string name, string title, float width = 0f, Action onClose = null)
        {
            var th = UiKit.Theme;
            var s = new UiSheet { Panel = UiPanel.Create(canvas, name, new Vector2(width > 0f ? width : th.dialogWidth, 400f), startHidden: true) };
            s.Panel.Modal = true;
            s.Panel.HasGrabber = false;
            UiNavigationBar.Create(s.Page, title, UiNavigationBar.Leading.Close, false, () =>
            {
                s.Close();
                onClose?.Invoke();
            });
            s.Panel.Closed += () => s.Closed?.Invoke();
            return s;
        }

        /// <summary>Measures the content and opens the sheet in place.</summary>
        public void Show()
        {
            Fit();
            Panel.Present();
        }

        /// <summary>Closes the sheet: the panels behind come forward, then it fades out.</summary>
        public void Close() => Panel.Close();

        /// <summary>Sets the surface height from the content.</summary>
        public void Fit()
        {
            var surface = (RectTransform)Panel.Surface.transform;
            var page = (RectTransform)Page.transform;
            Page.Finish();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            float h = LayoutUtility.GetPreferredHeight(page);
            surface.sizeDelta = new Vector2(surface.sizeDelta.x, h);
            LayoutRebuilder.ForceRebuildLayoutImmediate(page);
            Size = surface.sizeDelta;
        }
    }
}
