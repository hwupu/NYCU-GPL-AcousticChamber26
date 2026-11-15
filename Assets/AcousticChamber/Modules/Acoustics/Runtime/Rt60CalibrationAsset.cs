using System.Collections.Generic;
using UnityEngine;

namespace GPL.Acoustics
{
    /// <summary>
    /// A measured calibration table (reflection coefficient -> T_mid) for one room size on one platform, imported from a sweep report
    /// (<c>Magic > Acoustics > Import RT60 Calibration</c>). Kept in a <c>Resources/RT60Calibration</c> folder so every
    /// <see cref="ReverbTuner"/> finds it without links. The report it came from is named in <see cref="sourceReport"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "GPL/Acoustics/RT60 Calibration", fileName = "RT60Calibration")]
    public class Rt60CalibrationAsset : ScriptableObject
    {
        public const string ResourcesFolder = "RT60Calibration";

        public string platform = "Android";
        public string protocol = Rt60Calibration.ProtocolIso;
        public float roomWidth = Rt60Protocol.OpenSpaceWidth, roomDepth = Rt60Protocol.OpenSpaceDepth, roomHeight = Rt60Protocol.OpenSpaceHeight;
        [Tooltip("File name of the sweep report (CSV) the table was built from; the report is in docs/calibration.")]
        public string sourceReport = "";
        public string importedOn = "";
        public double[] reflection = new double[0];
        public double[] tmidSeconds = new double[0];

        public Rt60Calibration ToCalibration() =>
            new Rt60Calibration(reflection, tmidSeconds, roomWidth, roomDepth, roomHeight, sourceReport, platform, protocol);

        /// <summary>All imported tables in the build.</summary>
        public static List<Rt60Calibration> LoadAll()
        {
            var list = new List<Rt60Calibration>();
            foreach (var a in Resources.LoadAll<Rt60CalibrationAsset>(ResourcesFolder))
            {
                try { list.Add(a.ToCalibration()); }
                catch (System.Exception e) { Debug.LogWarning("[GPL] RT60 calibration " + a.name + " is unusable: " + e.Message); }
            }
            return list;
        }
    }

    /// <summary>Platform names used in calibration tables and reports.</summary>
    public static class Rt60Platform
    {
        /// <summary><c>Android</c> on the Quest; <c>Windows</c> in the Editor, a Windows player or over Link (Windows plugin).</summary>
        public static string Current
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android: return "Android";
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer: return "Windows";
                    default: return Application.platform.ToString();
                }
            }
        }
    }
}
