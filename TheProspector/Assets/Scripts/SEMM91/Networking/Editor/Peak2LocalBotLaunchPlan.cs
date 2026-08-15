using System;
using System.Collections.Generic;
using System.IO;

namespace SEMM91.Networking.EditorTools
{
    internal static class Peak2LocalBotLaunchPlan
    {
        public const int RequiredBotCount = 4;

        public static IReadOnlyList<
                Peak2LocalBotLaunchSpec>
            Create(
                string address,
                ushort port,
                int baseSeed,
                string logDirectory)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException(
                    "A bot connection address is required.",
                    nameof(address)
                );
            }

            if (port == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(port)
                );
            }

            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                throw new ArgumentException(
                    "A bot log directory is required.",
                    nameof(logDirectory)
                );
            }

            List<Peak2LocalBotLaunchSpec> result =
                new List<Peak2LocalBotLaunchSpec>(
                    RequiredBotCount
                );

            for (int index = 0;
                 index < RequiredBotCount;
                 index++)
            {
                int ordinal = index + 1;
                int seed = checked(baseSeed + index);

                string logPath = Path.GetFullPath(
                    Path.Combine(
                        logDirectory,
                        $"bot-{ordinal:00}.log"
                    )
                );

                string arguments =
                    BuildArguments(
                        address.Trim(),
                        port,
                        seed,
                        logPath
                    );

                result.Add(
                    new Peak2LocalBotLaunchSpec(
                        ordinal,
                        seed,
                        logPath,
                        arguments
                    )
                );
            }

            return result;
        }

        private static string BuildArguments(
            string address,
            ushort port,
            int seed,
            string logPath)
        {
            return
                "-mode=client " +
                $"-connect={Quote(address)} " +
                $"-port={port} " +
                "-bot=1 " +
                $"-botSeed={seed} " +
                "-exitOnDisconnect=1 " +
                "-targetFps=30 " +
                "-batchmode " +
                "-nographics " +
                $"-logFile {Quote(logPath)}";
        }

        private static string Quote(string value)
        {
            return
                "\"" +
                value.Replace("\"", "\\\"") +
                "\"";
        }
    }
}
