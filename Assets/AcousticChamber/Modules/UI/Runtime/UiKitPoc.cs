using UnityEngine;

namespace GPL.UI
{
    /// <summary>
    /// Proof of concept of the rebuilt UiKit (docs/design, UiPanel stories): the C01 experience chamber menu built only from
    /// layout-based components: UiPanel, UiSurface, UiNavigationBar, UiSection, UiRow, UiChoiceGroup, UiButton with icons, and the
    /// move handle. Headset check; not used by any chamber. Buttons at the bottom test hide / push back / placement.
    /// </summary>
    public class UiKitPoc : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(1000f, 820f);

        public UiPanel Panel { get; private set; }

        void Start() => Build();

        public void Build()
        {
            UiKit.Clear(transform);
            var th = UiKit.Theme;
            Panel = UiPanel.Create(transform, "Panel", Size);
            var surface = Panel.Surface;
            UiNavigationBar.Create(surface, "C01 · MovingSource", UiNavigationBar.Leading.Back, debugTag: false);

            var body = surface.Body;
            body.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 24f;

            var hearing = UiKit.Section(body, "Hearing", "HEARING");
            UiKit.ChoiceGroup(hearing, "Ear", new[] { "Both", "Left", "Right" }, 0, null);

            var path = UiKit.Section(body, "Path", "PATH");
            UiKit.Row(path, "Circle", "Circle", "Around the head at 2 m", iconLeading: "arrow-clockwise", onClick: () => { });
            UiKit.Row(path, "Fly-by", "Fly-by", "Straight past, tangent to 0° azimuth\nLeft to right", iconLeading: "arrow-right", onClick: () => { });
            UiKit.Row(path, "Approach", "Approach", null, "Near", iconLeading: "arrow-down", iconTrailing: "chevron-right", onClick: () => { });

            var tools = UiKit.Node(body, "Tools");
            UiKit.H(tools.gameObject, th.gap, null, TextAnchor.MiddleLeft);
            UiKit.Button(tools, "Config", "Config", UiStyle.Gray, iconLeading: "gear");
            UiKit.Button(tools, "Hide", "Hide 2 s", UiStyle.Borderless, minWidth: false, onClick: () => StartCoroutine(HideFor(2f)));
            UiKit.Button(tools, "Push", "Push back", UiStyle.Borderless, minWidth: false, onClick: () => Panel.Depth = Panel.Depth == 0 ? 1 : 0);
            UiKit.Button(tools, "Level", "Level / 8° below", UiStyle.Borderless, minWidth: false, onClick: () =>
                Panel.Placement = Panel.Placement == UiPanel.PlacementMode.Below8 ? UiPanel.PlacementMode.Level : UiPanel.PlacementMode.Below8);

            surface.Finish();
            var op = GetComponent<OverlayPanel>();
            if (op != null) op.Movable = true;
        }

        System.Collections.IEnumerator HideFor(float seconds)
        {
            Panel.Shown = false;
            yield return new WaitForSecondsRealtime(seconds);
            Panel.Shown = true;
        }
    }
}
