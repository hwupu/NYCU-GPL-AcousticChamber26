using System.Collections.Generic;
using UnityEngine;

namespace AcousticChamber.App
{
    /// <summary>
    /// The chambers shown in the Main Hall, in order (label, then ID). Written by <c>Magic > App > Sync Chambers</c>; do not
    /// edit by hand. Removing a chamber = delete its folder + Sync.
    /// </summary>
    public class ChamberCatalog : ScriptableObject
    {
        public List<ChamberDefinition> chambers = new List<ChamberDefinition>();

        /// <summary>Enabled chambers, in catalog order.</summary>
        public List<ChamberDefinition> Visible()
        {
            var list = new List<ChamberDefinition>();
            foreach (var c in chambers)
                if (c != null && c.enabled) list.Add(c);
            return list;
        }

        public ChamberDefinition Find(string id)
        {
            foreach (var c in chambers)
                if (c != null && c.id == id) return c;
            return null;
        }
    }
}
