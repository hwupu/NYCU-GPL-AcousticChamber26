using GPL.UI;
using TMPro;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// Stand-in for a chamber menu (PB-06a): the header with ‹ back to the Hall and the chamber's title, nothing else. The
    /// real chamber menu (Study ID, Start, Debug) replaces it in PB-07.
    /// </summary>
    public class ChamberStubPanel : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 700f);

        public ChamberDefinition chamber;

        RectTransform _window;

        void Start() => Build();

        public void Build()
        {
            UiStrings.EnsureRegistered();
            var th = UiKit.Theme;
            var canvas = (RectTransform)transform;
            UiKit.Clear(canvas);
            canvas.sizeDelta = Size;
            _window = UiKit.Window(canvas);
            var pages = PageStack.On(_window);
            pages.RootBack = Back;
            var page = pages.Push(chamber != null ? chamber.MenuTitle : "Chamber");
            UiKit.Text(page.Rect, "Note", "Stub chamber scene.\nThe chamber menu (Study ID, Start, Debug) comes with PB-07.\n‹ returns to the Main Hall.",
                       th.bodySize, new Vector2(0f, -40f), new Vector2(Page.ContentWidth(Size.x), 300f), color: th.text2,
                       align: TextAlignmentOptions.Center);
        }

        void Back()
        {
            if (AppNav.Busy) return;
            var layer = _window.GetComponent<UiLayer>();
            if (layer != null) layer.Show(false);
            if (!AppNav.BackToHall() && layer != null) layer.Show(true);
        }
    }
}
