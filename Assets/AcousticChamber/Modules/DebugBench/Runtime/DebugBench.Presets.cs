using System;
using GPL.Params;
using GPL.UI;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GPL.DebugBench
{
    /// <summary>
    /// The Presets page (DesignSystem section 7): Default first, then the saved presets; ✓ marks the active one. Tapping a
    /// preset opens a sheet with ×: <b>Make active</b> (it becomes the playground too; confirms first when that discards
    /// unsaved edits) and <b>Delete</b> (not Default; confirms; deleting the active preset makes Default active).
    /// </summary>
    public sealed partial class DebugBench
    {
        public const string ActiveMark = "✓"; // ✓ U+2713 (Noto Sans TC has it; not ✔)

        public UiPage OpenPresetsPage()
        {
            var page = _pages.Push("Presets");
            UiColumn col = null;

            void Build()
            {
                if (page == null) return;
                col?.Destroy();
                var th = UiKit.Theme;
                var store = Work.Store;
                string active = store.ActiveName;
                var names = store.List();

                col = UiColumn.In(page);
                col.Label("Real sessions use the active preset (" + ActiveMark + "). Tap a preset to make it active (it loads into the playground) or to delete Custom.");
                var g = col.Group("Presets");
                for (int i = 0; i < names.Count; i++)
                {
                    string name = names[i];
                    string sub = PresetSub(name);
                    var row = UiKit.Row(g, "Row " + name, name, sub.Length > 0 ? sub : null, name == active ? ActiveMark : "", null, null,
                                        () => OpenPresetSheet(name, Build));
                    var mark = UiKit.RowValue(row);
                    if (mark != null) mark.color = th.text;
                }
                col.Label("Saved on this device in " + store.Directory + ". Copy them off with the data.");
            }

            Build();
            page.Returned += Build;
            return page;
        }

        string PresetSub(string name)
        {
            string sub = name == PresetStore.DefaultName ? "Config/default.params · read-only" : "";
            if (Work.Source == name)
            {
                string wc = "playground" + (Work.IsEdited ? " · " + Work.EditedKeys.Count + " edited" : "");
                sub = sub.Length > 0 ? sub + " · " + wc : wc;
            }
            return sub;
        }

        /// <summary>The action sheet of one preset (× closes it without a choice).</summary>
        UiSheet OpenPresetSheet(string name, Action rebuild)
        {
            var th = UiKit.Theme;
            var store = Work.Store;
            bool active = store.ActiveName == name;
            bool isDefault = name == PresetStore.DefaultName;

            var sheet = UiSheet.Create(_canvas, "Preset " + name, name);
            string message = (active ? "Active: new real sessions use it, and the playground is based on it." : "Not active.") +
                             (isDefault ? " Read-only: it is Config/default.params in the project." : "");
            var msg = UiKit.Text(sheet.Body, "Message", message, th.smallSize, Vector2.zero, Vector2.zero, color: th.text2, align: TextAlignmentOptions.TopLeft);
            UiKit.Size(msg.gameObject, prefW: 0f, flexW: 1f);

            var spacer = UiKit.Node(sheet.Body, "Gap");
            UiKit.Size(spacer.gameObject, minH: 24f, prefH: 24f, flexH: 0f);
            var g = UiKit.Section(sheet.Body, "Actions");
            var makeActive = UiKit.Row(g, "Row Make active", "Make active", active ? "already active" : MakeActiveSub(), active ? ActiveMark : "", null, null,
                                       () => MakeActive(name, sheet, rebuild));
            makeActive.GetComponent<UiButton>().interactable = !active;

            if (!isDefault)
            {
                var delete = UiKit.Row(g, "Row Delete", "Delete", "removes the file from this device", null, null, null,
                                       () => ConfirmDeletePreset(name, sheet, rebuild));
                delete.GetComponent<UiButton>().Style = UiStyle.DestructiveText;
            }
            sheet.Show();
            return sheet;
        }

        string MakeActiveSub()
        {
            int n = Work.EditedKeys.Count;
            return "loads it into the playground" + (n > 0 ? " (" + n + " unsaved " + (n == 1 ? "edit" : "edits") + " are discarded)" : "");
        }

        /// <summary>Makes <paramref name="name"/> active and loads it; confirms first when unsaved edits would be lost.</summary>
        void MakeActive(string name, UiSheet sheet, Action rebuild)
        {
            void Go()
            {
                sheet.Close();
                try { Work.Activate(name); }
                catch (Exception e)
                {
                    SetStatus("Cannot load " + name + ": " + e.Message);
                    rebuild();
                    return;
                }
                foreach (var warning in Work.Warnings) Debug.LogWarning("[GPL] DebugBench: " + name + ": " + warning);
                SetStatus("Active preset: " + name + " (new real sessions use it)" +
                          (Work.Warnings.Count > 0 ? "; " + Work.Warnings.Count + " repairs, see the log" : ""));
                rebuild();
            }

            int n = Work.EditedKeys.Count;
            if (n == 0)
            {
                Go();
                return;
            }
            ModalDialog.Show(_canvas, "Discard " + n + (n == 1 ? " edit?" : " edits?"),
                "The playground has " + n + " unsaved " + (n == 1 ? "edit" : "edits") + " to " + Work.Source + ". Making " + name +
                " active loads it and discards " + (n == 1 ? "it" : "them") + " (" + SaveAsLabel + " keeps them).",
                new DialogButton("Cancel", UiStyle.Borderless),
                new DialogButton("Discard and switch", UiStyle.Destructive, Go));
        }

        void ConfirmDeletePreset(string name, UiSheet sheet, Action rebuild)
        {
            var store = Work.Store;
            bool active = store.ActiveName == name;
            string message = "The preset file is removed from this device.";
            if (active) message += " It is active: Default becomes active. Unfinished sessions keep their own snapshot.";
            if (Work.Source == name)
                message += " The playground loads " + (active ? PresetStore.DefaultName : store.ActiveName) +
                           (Work.IsEdited ? " (its " + Work.EditedKeys.Count + " unsaved edits are lost)" : "") + ".";
            ModalDialog.Show(_canvas, "Delete " + name + "?", message,
                new DialogButton("Cancel", UiStyle.Borderless),
                new DialogButton("Delete", UiStyle.Destructive, () =>
                {
                    sheet.Close();
                    if (Work.DeletePreset(name)) SetStatus("Deleted " + name + (active ? "; Default is active" : ""));
                    else SetStatus("Cannot delete " + name);
                    rebuild();
                }));
        }
    }
}
