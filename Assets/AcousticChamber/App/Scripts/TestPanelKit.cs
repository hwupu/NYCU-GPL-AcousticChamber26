using GPL.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AcousticChamber.App
{
    /// <summary>Layout helpers of the module test panels (Sandbox): a panel with a DEBUG title bar and text blocks.</summary>
    public static class TestPanelKit
    {
        /// <summary>A panel filling <paramref name="canvas"/> with a navigation bar showing <paramref name="title"/> and the DEBUG tag (no back button).</summary>
        public static UiPanel Panel(Transform canvas, Vector2 size, string title, bool debugTag = true)
        {
            UiKit.Clear(canvas);
            ((RectTransform)canvas).sizeDelta = size;
            var panel = UiPanel.Create(canvas, "Panel", size);
            UiNavigationBar.Create(panel.Surface, title, UiNavigationBar.Leading.None, debugTag);
            panel.Surface.Body.GetComponent<VerticalLayoutGroup>().spacing = 12f;
            return panel;
        }

        /// <summary>A wrapping text block across the parent's width (<c>text2</c> unless <paramref name="color"/> is given).</summary>
        public static TextMeshProUGUI Text(Transform parent, string name, string text, int size, Color? color = null, bool bold = false,
                                           bool flexHeight = false)
        {
            var t = UiKit.Text(parent, name, text, size, Vector2.zero, Vector2.zero, bold, color ?? UiKit.Theme.text,
                               TextAlignmentOptions.TopLeft);
            t.textWrappingMode = TextWrappingModes.Normal;
            UiKit.Size(t.gameObject, prefW: 0f, flexW: 1f, flexH: flexHeight ? 1f : -1f);
            return t;
        }

        /// <summary>A horizontal row of controls (<c>gap</c> apart).</summary>
        public static RectTransform Row(Transform parent, string name)
        {
            var row = UiKit.Node(parent, name);
            UiKit.H(row.gameObject, UiKit.Theme.gap, null, TextAnchor.MiddleLeft);
            UiKit.Size(row.gameObject, flexW: 1f);
            return row;
        }

        /// <summary>A vertical column (<paramref name="flex"/> = share of the row's width).</summary>
        public static RectTransform Column(Transform parent, string name, float spacing = 12f)
        {
            var col = UiKit.Node(parent, name);
            UiKit.V(col.gameObject, spacing, null, TextAnchor.UpperLeft);
            UiKit.Size(col.gameObject, prefW: 0f, flexW: 1f);
            return col;
        }

        /// <summary>A button that fills its cell's width.</summary>
        public static UiButton Wide(Transform parent, string name, string label, UiStyle style, System.Action onClick)
        {
            var th = UiKit.Theme;
            var b = UiKit.Button(parent, name, label, style, minWidth: false, onClick: onClick);
            UiKit.Size(b.gameObject, minW: 0f, minH: th.buttonHeight, prefW: 0f, prefH: th.buttonHeight, flexW: 1f, flexH: 0f);
            return b;
        }

        /// <summary>The label text of a button made by <see cref="UiKit.Button"/>.</summary>
        public static TextMeshProUGUI LabelOf(UiButton b) => b.GetComponentInChildren<TextMeshProUGUI>();
    }
}
