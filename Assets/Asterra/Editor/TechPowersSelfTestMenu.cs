#if UNITY_EDITOR
using System.IO;
using Asterra.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Asterra.Editor
{
    /// <summary>Headless tech/powers self-test from the Editor menu (no Play Mode required).</summary>
    public static class TechPowersSelfTestMenu
    {
        const string FlagPath = "/tmp/asterra-run-techpowers.flag";
        const string ResultPath = "/tmp/asterra-techpowers-result.txt";

        [MenuItem("Asterra/Run Tech Powers Self-Test")]
        public static void Run()
        {
            string report = TechPowersSelfTest.Run();
            Debug.Log(report);
            File.WriteAllText(ResultPath, report);
            bool ok = report.IndexOf("FAIL", System.StringComparison.Ordinal) < 0;
            EditorUtility.DisplayDialog(
                "Asterra Tech Powers",
                ok ? "OK — TechPowersSelfTest passed.\nSee Console for details."
                   : "FAIL — see Console / " + ResultPath,
                "Close");
        }

        [InitializeOnLoadMethod]
        static void AutoRunFromFlag()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(FlagPath))
                    return;
                try { File.Delete(FlagPath); } catch { /* ignore */ }
                string report = TechPowersSelfTest.Run();
                Debug.Log(report);
                File.WriteAllText(ResultPath, report + "\n");
            };
        }
    }
}
#endif

