using UnityEngine;

namespace Asterra.Gameplay.Analytics
{
    /// <summary>
    /// Reads Unity Cloud Build / CI environment markers when present.
    /// Safe no-op outside Cloud Build. Pair with Docs/CLOUD_BUILD.md for dashboard setup.
    /// </summary>
    public static class CloudBuildInfo
    {
        public static bool IsCloudBuild
        {
            get
            {
#if UNITY_CLOUD_BUILD
                return true;
#else
                return !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("UNITY_CLOUD_BUILD_NUMBER"))
                    || !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("BUILD_NUMBER"));
#endif
            }
        }

        public static string BuildNumber =>
            FirstNonEmpty(
                System.Environment.GetEnvironmentVariable("UNITY_CLOUD_BUILD_NUMBER"),
                System.Environment.GetEnvironmentVariable("BUILD_NUMBER"),
                Application.version);

        public static string CommitSha =>
            FirstNonEmpty(
                System.Environment.GetEnvironmentVariable("UNITY_CLOUD_COMMIT"),
                System.Environment.GetEnvironmentVariable("GIT_COMMIT"),
                "local");

        public static string TargetName =>
            FirstNonEmpty(
                System.Environment.GetEnvironmentVariable("UNITY_CLOUD_TARGET_NAME"),
                "editor-local");

        public static void LogBootstrap()
        {
            if (!IsCloudBuild)
            {
                Debug.Log($"[Asterra CloudBuild stub] local build version={Application.version}");
                return;
            }

            Debug.Log(
                $"[Asterra CloudBuild] target={TargetName} build={BuildNumber} commit={CommitSha} unity={Application.unityVersion}");
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
                return string.Empty;
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]))
                    return values[i];
            }

            return string.Empty;
        }
    }
}
