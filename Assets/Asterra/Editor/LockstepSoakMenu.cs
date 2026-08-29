#if UNITY_EDITOR
using Asterra.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Asterra.Editor
{
    /// <summary>Headless 2→8 lockstep soak from the Editor menu (no Play Mode required).</summary>
    public static class LockstepSoakMenu
    {
        [MenuItem("Asterra/Run Lockstep Soak (2→8)")]
        public static void Run()
        {
            string dual = DualSimSoakSelfTest.Run();
            string lockstep = LockstepSoakSelfTest.Run();
            Debug.Log(dual);
            Debug.Log(lockstep);
            bool ok = dual.IndexOf("FAIL", System.StringComparison.Ordinal) < 0
                      && lockstep.IndexOf("FAIL", System.StringComparison.Ordinal) < 0;
            if (ok)
                EditorUtility.DisplayDialog(
                    "Asterra Lockstep Soak",
                    "OK — dual-sim + 2/4/8 gate + loopback session passed.\nSee Console for details.",
                    "Close");
            else
                EditorUtility.DisplayDialog(
                    "Asterra Lockstep Soak",
                    "FAIL — see Console for DualSimSoakSelfTest / LockstepSoakSelfTest output.",
                    "Close");
        }

        /// <summary>Unity batchmode: -executeMethod Asterra.Editor.LockstepSoakMenu.RunFromCommandLine</summary>
        public static void RunFromCommandLine()
        {
            string dual = DualSimSoakSelfTest.Run();
            string lockstep = LockstepSoakSelfTest.Run();
            string mutations = WorldMutationSelfTest.Run();
            string content = ContentAndEconomySelfTest.Run();
            Debug.Log(dual);
            Debug.Log(lockstep);
            Debug.Log(mutations);
            Debug.Log(content);
            bool ok = dual.IndexOf("FAIL", System.StringComparison.Ordinal) < 0
                      && lockstep.IndexOf("FAIL", System.StringComparison.Ordinal) < 0
                      && mutations.IndexOf("FAIL", System.StringComparison.Ordinal) < 0
                      && content.IndexOf("FAIL", System.StringComparison.Ordinal) < 0;
            EditorApplication.Exit(ok ? 0 : 1);
        }
    }
}
#endif
