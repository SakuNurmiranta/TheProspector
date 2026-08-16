using System;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace SEMM91.Networking.EditorTools
{
    internal sealed class Peak2LocalBotSwarmWindow :
        EditorWindow
    {
        private const string ExecutablePreferenceKey =
            "SEMM91.Peak2.BotClientExecutable";

        private string _clientExecutablePath;
        private string _address = "127.0.0.1";
        private int _port = 7777;
        private int _baseSeed = 24000;

        [MenuItem(
            "Demo(N)Tapes/Peak 2/Local Bot Swarm")]
        private static void OpenWindow()
        {
            Peak2LocalBotSwarmWindow window =
                GetWindow<Peak2LocalBotSwarmWindow>();

            window.titleContent =
                new GUIContent("Peak 2 Bots");

            window.minSize =
                new Vector2(460f, 300f);

            window.Show();
        }

        private void OnEnable()
        {
            _clientExecutablePath =
                EditorPrefs.GetString(
                    ExecutablePreferenceKey,
                    string.Empty
                );
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField(
                "Peak 2 Local Bot Swarm",
                EditorStyles.boldLabel
            );

            EditorGUILayout.HelpBox(
                "Launches exactly four real headless " +
                "Netcode clients. Build the Windows client " +
                "after every replicated-schema change.",
                MessageType.Info
            );

            DrawExecutableField();

            _address = EditorGUILayout.TextField(
                "Host Address",
                _address
            );

            _port = EditorGUILayout.IntField(
                "Port",
                _port
            );

            _baseSeed = EditorGUILayout.IntField(
                "Base Bot Seed",
                _baseSeed
            );

            EditorGUILayout.Space();

            bool hostReady =
                TryValidateHostRoster(
                    out string hostStatus
                );

            EditorGUILayout.HelpBox(
                hostStatus,
                hostReady
                    ? MessageType.None
                    : MessageType.Warning
            );

            int runningCount =
                Peak2LocalBotProcessRegistry
                    .RunningCount;

            EditorGUILayout.LabelField(
                "Launched Bots",
                runningCount.ToString()
            );

            using (new EditorGUI.DisabledScope(
                       !hostReady || runningCount > 0))
            {
                if (GUILayout.Button(
                        "Launch Four Local Bots",
                        GUILayout.Height(34f)))
                {
                    LaunchBots();
                }
            }

            using (new EditorGUI.DisabledScope(
                       runningCount == 0))
            {
                if (GUILayout.Button(
                        "Stop Launched Bots"))
                {
                    Peak2LocalBotProcessRegistry
                        .StopAll();
                }
            }
        }

        private void DrawExecutableField()
        {
            EditorGUILayout.BeginHorizontal();

            _clientExecutablePath =
                EditorGUILayout.TextField(
                    "Client Executable",
                    _clientExecutablePath
                );

            if (GUILayout.Button(
                    "Browse",
                    GUILayout.Width(72f)))
            {
                string initialDirectory =
                    ResolveInitialBrowseDirectory();

                string selected =
                    EditorUtility.OpenFilePanel(
                        "Select current Demo(N)Tapes " +
                        "Windows client",
                        initialDirectory,
                        "exe"
                    );

                if (!string.IsNullOrWhiteSpace(
                        selected))
                {
                    _clientExecutablePath = selected;

                    EditorPrefs.SetString(
                        ExecutablePreferenceKey,
                        selected
                    );
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private string ResolveInitialBrowseDirectory()
        {
            if (!string.IsNullOrWhiteSpace(
                    _clientExecutablePath))
            {
                string directory =
                    Path.GetDirectoryName(
                        _clientExecutablePath
                    );

                if (!string.IsNullOrWhiteSpace(
                        directory) &&
                    Directory.Exists(directory))
                {
                    return directory;
                }
            }

            return Directory.GetParent(
                       Application.dataPath
                   )?.FullName ??
                   Application.dataPath;
        }

        private bool TryValidateHostRoster(
            out string status)
        {
            if (!Application.isPlaying)
            {
                status =
                    "Enter Play Mode and start Local Host " +
                    "before launching bots.";
                return false;
            }

            NetworkManager networkManager =
                NetworkManager.Singleton;

            if (networkManager == null ||
                !networkManager.IsHost ||
                !networkManager.IsListening)
            {
                status =
                    "This Editor instance is not the " +
                    "listening Local Host.";
                return false;
            }

            GameCoordinator coordinator =
                GameCoordinator.Instance;

            if (coordinator == null ||
                !coordinator.IsSpawned)
            {
                status =
                    "Waiting for the GameCoordinator to spawn.";
                return false;
            }

            if (coordinator.testStarted.Value)
            {
                status =
                    "The playable session has already started.";
                return false;
            }

            int connected =
                coordinator
                    .sessionConnectedPlayerCount.Value;

            int ready =
                coordinator
                    .sessionReadyPlayerCount.Value;

            int humans =
                coordinator
                    .sessionHumanPlayerCount.Value;

            int bots =
                coordinator
                    .sessionBotPlayerCount.Value;

            if (connected != 1 ||
                ready != 1 ||
                humans != 1 ||
                bots != 0)
            {
                status =
                    "Stop the Parrel client first. The bot " +
                    "swarm requires the host to be the sole " +
                    "participant | " +
                    $"connected={connected}, ready={ready}, " +
                    $"humans={humans}, bots={bots}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    _clientExecutablePath) ||
                !File.Exists(_clientExecutablePath))
            {
                status =
                    "Select the current Windows client " +
                    "executable.";
                return false;
            }

            if (_port < 1 || _port > ushort.MaxValue)
            {
                status = "Port must be between 1 and 65535.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_address))
            {
                status = "Host Address cannot be empty.";
                return false;
            }

            status =
                "Host gate ready | connected=1/5, " +
                "ready=1, humans=1, bots=0.";

            return true;
        }

        private void LaunchBots()
        {
            try
            {
                string projectRoot =
                    Directory.GetParent(
                        Application.dataPath
                    )?.FullName ??
                    Application.dataPath;

                string logDirectory =
                    Path.Combine(
                        projectRoot,
                        "Logs",
                        "Peak2LocalBots"
                    );

                IReadOnlyList<
                        Peak2LocalBotLaunchSpec>
                    launchPlan =
                        Peak2LocalBotLaunchPlan.Create(
                            _address,
                            (ushort)_port,
                            _baseSeed,
                            logDirectory
                        );

                Peak2LocalBotProcessRegistry.Launch(
                    _clientExecutablePath,
                    launchPlan
                );

                Debug.Log(
                    "[PEAK2 BOT SWARM] Four local bots " +
                    "launched. Waiting for the authoritative " +
                    "1-human/4-bot gate."
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[PEAK2 BOT SWARM] Launch failed | " +
                    exception
                );

                EditorUtility.DisplayDialog(
                    "Peak 2 Bot Launch Failed",
                    exception.Message,
                    "OK"
                );
            }
        }
    }
}
