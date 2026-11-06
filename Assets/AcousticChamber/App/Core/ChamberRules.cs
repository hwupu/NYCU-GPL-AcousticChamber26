using GPL.Core;

namespace AcousticChamber.App
{
    /// <summary>
    /// Rules a chamber must follow to enter the catalog (checked by <c>Magic > App > Sync Chambers</c>). The ID is part of
    /// data file names that are split on '_' (<c>yyyyMMdd_HHmmss_&lt;ChamberId&gt;_S0001</c>), so it has letters, digits
    /// and '-' only. The folder is the ID, or '_' + ID for folders that sort first (<c>_Template</c> holds <c>Template</c>).
    /// </summary>
    public static class ChamberRules
    {
        /// <summary>Null if the chamber is fine, otherwise what is wrong.</summary>
        public static string Problem(string id, string folderName)
        {
            if (string.IsNullOrEmpty(id)) return "the id is empty";
            if (Ids.SanitizeToken(id) != id) return "the id '" + id + "' may contain only letters, digits and '-'";
            if (folderName != id && folderName != "_" + id)
                return "the folder '" + folderName + "' must be named '" + id + "' or '_" + id + "'";
            return null;
        }

        /// <summary>Hall title of a card or chamber menu: "C04 · HumPerHW2" (the label is optional).</summary>
        public static string MenuTitle(string label, string id) => string.IsNullOrEmpty(label) ? id : label + " · " + id;
    }
}
