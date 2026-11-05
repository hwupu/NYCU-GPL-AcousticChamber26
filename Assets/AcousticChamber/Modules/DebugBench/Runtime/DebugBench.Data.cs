using System;
using GPL.Session;
using GPL.UI;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GPL.DebugBench
{
    /// <summary>
    /// The Data files page (DesignSystem section 7): one row per session of the chamber (its CSV and <c>.session</c> file;
    /// Debug runs: CSV only), newest first, with a red Delete at the right. Delete asks first; <b>Delete all</b> (header)
    /// asks once and names the count. Only the chamber's own data files are touched (<see cref="DataFiles"/>).
    /// </summary>
    public sealed partial class DebugBench
    {
        /// <summary>The chamber's data folder (<c>persistentDataPath/&lt;ChamberId&gt;/</c>); null: the Data files row is disabled.</summary>
        public DataFiles Files { get; set; }

        TextMeshProUGUI _dataValue;

        /// <summary>"12 files", "1 file", or "none".</summary>
        public static string CountText(int n) => n == 0 ? "none" : n + (n == 1 ? " file" : " files");

        string DataSummary()
        {
            if (Files == null) return "not set";
            try { return CountText(Files.List().Count); }
            catch (Exception e) { return "unreadable: " + e.Message; }
        }

        public Page OpenDataPage()
        {
            if (Files == null) throw new InvalidOperationException("DebugBench.Files is not set.");
            var page = _pages.Push("Data files");
            Column col = null;
            UiButton deleteAll = null;
            deleteAll = page.AddHeaderAction("Delete all", UiStyle.DestructiveText, () => ConfirmDeleteAll(Build));

            void Build()
            {
                if (page == null) return;
                if (col != null)
                {
                    var old = col.List.gameObject;
                    old.SetActive(false);
                    if (Application.isPlaying) Object.Destroy(old);
                    else Object.DestroyImmediate(old);
                }
                var entries = Files.List();
                deleteAll.interactable = entries.Count > 0;

                col = Column.In(page);
                col.Label(entries.Count == 0
                    ? "No data files of this chamber on this device."
                    : CountText(entries.Count) + " on this device, newest first. Copy them off before deleting: deleting cannot be undone.");
                if (entries.Count > 0)
                {
                    const float deleteW = 140f;
                    var g = col.Group("Files", entries.Count);
                    for (int i = 0; i < entries.Count; i++)
                    {
                        var e = entries[i];
                        float right = deleteW + 24f + 16f;
                        // one line: the file name, centered; a long one shrinks instead of being cut (the _DEBUG suffix stays visible)
                        var row = UiKit.InfoRow(g, i, e.BaseName, null, null, right);
                        var label = UiKit.RowText(row, "Label");
                        label.enableAutoSizing = true;
                        label.fontSizeMin = UiKit.Theme.minimumSize;
                        label.fontSizeMax = UiKit.Theme.bodySize;
                        label.rectTransform.offsetMax = new Vector2(-right, label.rectTransform.offsetMax.y);
                        var del = UiKit.SmallButton(row, "Delete", "Delete", new Vector2(row.sizeDelta.x / 2f - 24f - deleteW / 2f, 0f), deleteW,
                                                    () => ConfirmDelete(e, Build), UiStyle.DestructiveText);
                        del.InGroup = true;
                    }
                }
                col.Label("Folder: " + Files.Folder + ". Presets (presets/) are not data files and stay.");
                col.Finish();
                RefreshDebugPage();
            }

            Build();
            page.Returned += Build;
            return page;
        }


        void ConfirmDelete(DataEntry e, Action rebuild)
        {
            string message = (e.SessionPath != null ? "The CSV and session file are" : "The CSV is") +
                             " removed from this device. This cannot be undone.";
            if (e.Status == SessionStatus.InProgress)
                message += " The session is unfinished: " + (e.StudyId.Length > 0 ? e.StudyId : "it") + " can no longer resume it.";
            ModalDialog.Show(_canvas, "Delete " + (e.IsDebug ? "this Debug run" : e.StudyId.Length > 0 ? e.StudyId + "'s session" : "this session") + "?", message,
                new DialogButton("Cancel", UiStyle.Borderless),
                new DialogButton("Delete", UiStyle.Destructive, () =>
                {
                    try
                    {
                        int n = Files.Delete(e);
                        SetStatus("Deleted " + e.BaseName + " (" + n + (n == 1 ? " file)" : " files)"));
                    }
                    catch (Exception ex) { SetStatus("Cannot delete " + e.BaseName + ": " + ex.Message); }
                    rebuild();
                }));
        }

        /// <summary>Delete all: one confirmation naming the count (PO: no second confirmation).</summary>
        void ConfirmDeleteAll(Action rebuild)
        {
            var entries = Files.List();
            int n = entries.Count;
            if (n == 0) return;
            int unfinished = entries.FindAll(x => x.Status == SessionStatus.InProgress).Count;
            string what = CountText(n);
            ModalDialog.Show(_canvas, "Delete all " + what + "?",
                "Every data file of this chamber on this device" +
                (unfinished > 0 ? ", including " + unfinished + " unfinished " + (unfinished == 1 ? "session" : "sessions") + " (no resume)" : "") +
                ". This cannot be undone.",
                new DialogButton("Cancel", UiStyle.Borderless),
                new DialogButton("Delete " + what, UiStyle.Destructive, () =>
                {
                    try { SetStatus("Deleted " + CountText(Files.DeleteAll())); }
                    catch (Exception ex) { SetStatus("Cannot delete all: " + ex.Message); }
                    rebuild();
                }));
        }
    }
}
