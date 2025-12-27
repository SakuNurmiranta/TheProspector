using UnityEngine;
namespace SEMM91
{
    public static class RunLog
    {
        public const string BuildVersion = "0.0.1"; //update by hand

        public static void Header(string role, string testCase, string preset, int clientsPlanned, int botSeed)
        {
            Debug.Log(
                $"[RUN] build={BuildVersion} role={role} tc={testCase} preset={preset} plannedClients={clientsPlanned} botSeed={botSeed} " +
                $"unityTime={Time.realtimeSinceStartup:F2}");
        }
    }
}
