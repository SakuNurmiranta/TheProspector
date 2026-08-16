using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace SEMM91.Networking.EditorTools
{
    [InitializeOnLoad]
    internal static class Peak2LocalBotProcessRegistry
    {
        private static readonly List<Process>
            Processes = new List<Process>();

        static Peak2LocalBotProcessRegistry()
        {
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;

            AssemblyReloadEvents.beforeAssemblyReload +=
                StopAll;

            EditorApplication.quitting += StopAll;
        }

        public static int RunningCount
        {
            get
            {
                RemoveExitedProcesses();
                return Processes.Count;
            }
        }

        public static void Launch(
            string executablePath,
            IReadOnlyList<Peak2LocalBotLaunchSpec>
                launchSpecs)
        {
            if (string.IsNullOrWhiteSpace(
                    executablePath) ||
                !File.Exists(executablePath))
            {
                throw new FileNotFoundException(
                    "The selected bot client executable " +
                    "does not exist.",
                    executablePath
                );
            }

            if (launchSpecs == null ||
                launchSpecs.Count !=
                Peak2LocalBotLaunchPlan
                    .RequiredBotCount)
            {
                throw new ArgumentException(
                    "Peak 2 requires exactly four bot " +
                    "launch specifications.",
                    nameof(launchSpecs)
                );
            }

            StopAll();

            try
            {
                foreach (
                    Peak2LocalBotLaunchSpec spec
                    in launchSpecs)
                {
                    string logDirectory =
                        Path.GetDirectoryName(
                            spec.LogPath
                        );

                    if (!string.IsNullOrWhiteSpace(
                            logDirectory))
                    {
                        Directory.CreateDirectory(
                            logDirectory
                        );
                    }

                    ProcessStartInfo startInfo =
                        new ProcessStartInfo
                        {
                            FileName = executablePath,
                            Arguments = spec.Arguments,
                            WorkingDirectory =
                                Path.GetDirectoryName(
                                    executablePath
                                ) ?? string.Empty,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                    Process process =
                        Process.Start(startInfo);

                    if (process == null)
                    {
                        throw new InvalidOperationException(
                            $"Bot {spec.BotOrdinal} did not " +
                            "produce a process."
                        );
                    }

                    Processes.Add(process);

                    Debug.Log(
                        "[PEAK2 BOT SWARM] Launched | " +
                        $"bot={spec.BotOrdinal}/" +
                        $"{Peak2LocalBotLaunchPlan.RequiredBotCount} | " +
                        $"pid={process.Id} | " +
                        $"seed={spec.Seed} | " +
                        $"log={spec.LogPath}"
                    );
                }
            }
            catch
            {
                StopAll();
                throw;
            }
        }

        public static void StopAll()
        {
            StopProcesses(Processes);
            Processes.Clear();
        }

        private static void StopProcesses(
            IReadOnlyList<Process> processes)
        {
            if (processes == null)
                return;

            for (int index = processes.Count - 1;
                 index >= 0;
                 index--)
            {
                Process process = processes[index];

                if (process == null)
                    continue;

                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[PEAK2 BOT SWARM] Could not stop " +
                        $"process | {exception.Message}"
                    );
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        private static void RemoveExitedProcesses()
        {
            for (int index = Processes.Count - 1;
                 index >= 0;
                 index--)
            {
                Process process = Processes[index];

                if (process != null &&
                    !process.HasExited)
                {
                    continue;
                }

                process?.Dispose();
                Processes.RemoveAt(index);
            }
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state ==
                    PlayModeStateChange.ExitingPlayMode ||
                state ==
                    PlayModeStateChange.EnteredEditMode)
            {
                StopAll();
            }
        }
    }
}
