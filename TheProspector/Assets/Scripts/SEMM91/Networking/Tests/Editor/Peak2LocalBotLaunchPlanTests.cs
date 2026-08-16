using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SEMM91.Networking.EditorTools;

namespace SEMM91.Networking.Tests.Editor
{
    public class Peak2LocalBotLaunchPlanTests
    {
        [Test]
        public void CreateUsesExactlyFourBotParticipants()
        {
            IReadOnlyList<Peak2LocalBotLaunchSpec> plan =
                CreatePlan();

            Assert.That(
                plan.Count,
                Is.EqualTo(4)
            );
        }

        [Test]
        public void CreateAssignsDeterministicUniqueSeeds()
        {
            int[] seeds =
                CreatePlan()
                    .Select(spec => spec.Seed)
                    .ToArray();

            Assert.That(
                seeds,
                Is.EqualTo(
                    new[]
                    {
                        24000,
                        24001,
                        24002,
                        24003
                    }
                )
            );
        }

        [Test]
        public void ArgumentsStartHeadlessBotClientAtEndpoint()
        {
            string arguments =
                CreatePlan()[0].Arguments;

            Assert.That(
                arguments,
                Does.Contain("-mode=client")
            );

            Assert.That(
                arguments,
                Does.Contain("-connect=\"127.0.0.1\"")
            );

            Assert.That(
                arguments,
                Does.Contain("-port=7777")
            );

            Assert.That(
                arguments,
                Does.Contain("-bot=1")
            );

            Assert.That(
                arguments,
                Does.Contain("-exitOnDisconnect=1")
            );

            Assert.That(
                arguments,
                Does.Contain("-batchmode")
            );

            Assert.That(
                arguments,
                Does.Contain("-nographics")
            );
        }

        [Test]
        public void EachBotReceivesDistinctQuotedLogPath()
        {
            IReadOnlyList<Peak2LocalBotLaunchSpec> plan =
                CreatePlan();

            Assert.That(
                plan.Select(spec => spec.LogPath)
                    .Distinct()
                    .Count(),
                Is.EqualTo(4)
            );

            foreach (
                Peak2LocalBotLaunchSpec spec
                in plan)
            {
                Assert.That(
                    spec.Arguments,
                    Does.Contain(
                        $"-logFile \"{spec.LogPath}\""
                    )
                );
            }
        }

        [Test]
        public void InvalidConfigurationIsRejected()
        {
            Assert.Throws<ArgumentException>(
                () => Peak2LocalBotLaunchPlan.Create(
                    string.Empty,
                    7777,
                    24000,
                    "Logs"
                )
            );

            Assert.Throws<ArgumentOutOfRangeException>(
                () => Peak2LocalBotLaunchPlan.Create(
                    "127.0.0.1",
                    0,
                    24000,
                    "Logs"
                )
            );

            Assert.Throws<ArgumentException>(
                () => Peak2LocalBotLaunchPlan.Create(
                    "127.0.0.1",
                    7777,
                    24000,
                    string.Empty
                )
            );
        }

        private static IReadOnlyList<
                Peak2LocalBotLaunchSpec>
            CreatePlan()
        {
            return Peak2LocalBotLaunchPlan.Create(
                "127.0.0.1",
                7777,
                24000,
                "Logs With Spaces"
            );
        }
    }
}
