using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// A sheet (DesignSystem section 6): a <c>surface</c> box that appears in place on a canvas (scale + fade, no slide)
    /// and pushes every layer behind it back (window 94 %, two levels down 88 %) and dims it. Sheets stack: a second sheet
    /// pushes back the first one and the window. Only the top sheet takes the ray (<see cref="UiPointer.Modal"/>).
    /// Build the content under <see cref="Rect"/>; <see cref="ModalDialog"/> is a sheet with a title, a message and buttons.
    /// Without a window on the canvas the sheet simply appears alone.
    /// </summary>
    [ExecuteAlways] // OnDestroy (Abandoned) also in edit mode / EditMode tests
    public class Sheet : MonoBehaviour
    {
        UiLayer _layer;
        Transform _canvas;
        bool _closed;

        /// <summary>The sheet's box; put the content here.</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>True once <see cref="Close"/> was called (it may still be fading out).</summary>
        public bool IsClosing => _closed;

        /// <summary>Raised by <see cref="Close"/>, after the layers behind have been brought forward.</summary>
        public event Action Closed;

        /// <summary>Raised when the sheet is destroyed without <see cref="Close"/> (e.g. its canvas or scene unloaded).</summary>
        public event Action Abandoned;

        /// <summary>Opens a sheet of <paramref name="size"/> at <paramref name="pos"/> (canvas units) on top of <paramref name="canvas"/>.</summary>
        public static Sheet Open(Transform canvas, string name, Vector2 pos, Vector2 size)
        {
            var th = UiKit.Theme;
            var box = UiKit.Box(canvas, name, pos, size, th.surface);
            var sheet = box.gameObject.AddComponent<Sheet>();
            sheet._canvas = canvas;
            sheet._layer = UiLayer.Attach(box.rectTransform, th.panelRadius, shown: false);
            Restack(canvas);
            sheet._layer.Show(true);
            return sheet;
        }

        /// <summary>The open sheets of <paramref name="canvas"/>, bottom to top (closing sheets excluded).</summary>
        public static List<Sheet> Stack(Transform canvas)
        {
            var list = new List<Sheet>();
            if (canvas == null) return list;
            foreach (Transform child in canvas)
            {
                var s = child.GetComponent<Sheet>();
                if (s != null && !s._closed) list.Add(s);
            }
            return list;
        }

        /// <summary>The top open sheet of <paramref name="canvas"/>, or null.</summary>
        public static Sheet Top(Transform canvas)
        {
            var stack = Stack(canvas);
            return stack.Count > 0 ? stack[stack.Count - 1] : null;
        }

        /// <summary>
        /// Adds × close at the top left of the sheet (for sheets that may be left without a choice; decision sheets have
        /// Cancel instead). <paramref name="onClose"/> runs after the sheet closed.
        /// </summary>
        public UiButton AddCloseButton(Action onClose = null)
        {
            var th = UiKit.Theme;
            var size = Rect.rect.size;
            float d = th.iconButtonSize;
            var b = UiKit.IconButton(Rect, "Close", UiKit.CloseGlyph,
                                     new Vector2(-size.x / 2f + Page.EdgePad + d / 2f, size.y / 2f - Page.TopPad - d / 2f), () =>
                                     {
                                         Close();
                                         onClose?.Invoke();
                                     });
            return b;
        }

        /// <summary>Closes the sheet: the layers behind come forward, then it fades out and is destroyed.</summary>
        public void Close()
        {
            if (this == null || _closed) return;
            _closed = true;
            Restack(_canvas);
            Closed?.Invoke();
            if (_layer != null) _layer.Show(false, DestroySelf);
            else DestroySelf();
        }

        void DestroySelf()
        {
            if (this == null) return;
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        void OnDestroy()
        {
            if (_closed) return;
            _closed = true;
            if (_canvas != null) Restack(_canvas);
            else if (UiPointer.Modal == transform) UiPointer.Modal = null;
            Abandoned?.Invoke();
        }

        /// <summary>
        /// Gives every layer of <paramref name="canvas"/> its depth (the window and open sheets; the top one is at 0) and
        /// points <see cref="UiPointer.Modal"/> at the top sheet.
        /// </summary>
        static void Restack(Transform canvas)
        {
            if (canvas == null) return;
            var layers = new List<UiLayer>();
            Sheet top = null;
            foreach (Transform child in canvas)
            {
                var layer = child.GetComponent<UiLayer>();
                if (layer == null) continue;
                var s = child.GetComponent<Sheet>();
                if (s != null)
                {
                    if (s._closed) continue;
                    top = s;
                }
                layers.Add(layer);
            }
            for (int i = 0; i < layers.Count; i++) layers[i].SetDepth(layers.Count - 1 - i);

            if (top != null) UiPointer.Modal = top.transform;
            else if (UiPointer.Modal == null || UiPointer.Modal.IsChildOf(canvas)) UiPointer.Modal = null;
        }
    }
}
