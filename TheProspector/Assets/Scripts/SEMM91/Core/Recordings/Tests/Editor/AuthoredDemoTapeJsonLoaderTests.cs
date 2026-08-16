using System;
using NUnit.Framework;
using SEMM91.Core.Ideas;
using SEMM91.Core.Tags;
using SEMM91.Core.Tracks;
using UnityEditor;
using UnityEngine;

namespace SEMM91.Core.Recordings.Tests.Editor
{
    public sealed class
        AuthoredDemoTapeJsonLoaderTests
    {
        private const string FreezingMoonPath =
            "Assets/Scripts/SEMM91/GamePlay/" +
            "Kvlt/Scenario/Data/FreezingMoon.json";

        [Test]
        public void
            FreezingMoonBuildsThreeTracksAndNineIdeas()
        {
            DemoTape tape =
                LoadFreezingMoon();

            Assert.That(
                tape.DemoTapeId,
                Is.EqualTo(
                    "HISTORICAL_DEMO_MAYHEM_FREEZING_MOON"
                )
            );

            Assert.That(
                tape.DisplayName,
                Is.EqualTo("Freezing Moon")
            );

            Assert.That(
                tape.TrackSnapshots.Count,
                Is.EqualTo(3)
            );

            int ideaCount =
                0;

            foreach (
                DemoTapeTrackSnapshot track
                in tape.TrackSnapshots)
            {
                Assert.That(
                    track.IdeaSnapshots.Count,
                    Is.EqualTo(3)
                );

                ideaCount +=
                    track.IdeaSnapshots.Count;
            }

            Assert.That(
                ideaCount,
                Is.EqualTo(9)
            );
        }

        [Test]
        public void
            FreezingMoonPreservesThreePairsAndSixSolitaryIdeas()
        {
            DemoTape tape =
                LoadFreezingMoon();

            int pairCount =
                0;

            int solitaryCount =
                0;

            foreach (
                DemoTapeTrackSnapshot track
                in tape.TrackSnapshots)
            {
                foreach (
                    DemoTapeIdeaSnapshot idea
                    in track.IdeaSnapshots)
                {
                    switch (idea.PayloadType)
                    {
                        case IdeaPayloadType.TagPair:
                            pairCount++;

                            Assert.That(
                                idea.TagOccurrences.Count,
                                Is.EqualTo(2)
                            );

                            Assert.That(
                                idea.TagOccurrences[0].Role,
                                Is.EqualTo(
                                    DemoTapeTagOccurrenceRole
                                        .PairDominant
                                )
                            );

                            Assert.That(
                                idea.TagOccurrences[1].Role,
                                Is.EqualTo(
                                    DemoTapeTagOccurrenceRole
                                        .PairSubmissive
                                )
                            );

                            break;

                        case IdeaPayloadType.SingleTag:
                            solitaryCount++;

                            Assert.That(
                                idea.TagOccurrences.Count,
                                Is.EqualTo(1)
                            );

                            Assert.That(
                                idea.TagOccurrences[0].Role,
                                Is.EqualTo(
                                    DemoTapeTagOccurrenceRole
                                        .Solitary
                                )
                            );

                            break;
                    }
                }
            }

            Assert.That(
                pairCount,
                Is.EqualTo(3)
            );

            Assert.That(
                solitaryCount,
                Is.EqualTo(6)
            );
        }

        [Test]
        public void
            FreezingMoonPairsAreProfaneMorbidAndMalevolentAtDegreeOne()
        {
            DemoTape tape =
                LoadFreezingMoon();

            AssertPair(
                tape.TrackSnapshots[0]
                    .IdeaSnapshots[0],
                TagAxis.Symbolic
            );

            AssertPair(
                tape.TrackSnapshots[1]
                    .IdeaSnapshots[0],
                TagAxis.Existential
            );

            AssertPair(
                tape.TrackSnapshots[2]
                    .IdeaSnapshots[0],
                TagAxis.Physical
            );
        }

        [Test]
        public void
            TrackNamesAreGeneratedFromCompositionAndAreDeterministic()
        {
            DemoTape first =
                LoadFreezingMoon();

            DemoTape second =
                LoadFreezingMoon();

            for (int index = 0;
                 index < first.TrackSnapshots.Count;
                 index++)
            {
                string firstName =
                    first.TrackSnapshots[index]
                        .DisplayName;

                string secondName =
                    second.TrackSnapshots[index]
                        .DisplayName;

                Assert.That(
                    firstName,
                    Is.Not.Null.And.Not.Empty
                );

                Assert.That(
                    firstName,
                    Is.Not.EqualTo(
                        "Untitled Track"
                    )
                );

                Assert.That(
                    secondName,
                    Is.EqualTo(firstName)
                );
            }
        }

        [Test]
        public void
            SourceEntityAndTurnComeFromRuntimeContext()
        {
            DemoTape tape =
                LoadFreezingMoon(
                    sourceEntityId:
                        "ENTITY_MAYHEM_RUNTIME",
                    recordedTurn:
                        0
                );

            Assert.That(
                tape.RecordedTurn,
                Is.EqualTo(0)
            );

            foreach (
                DemoTapeTrackSnapshot track
                in tape.TrackSnapshots)
            {
                foreach (
                    DemoTapeIdeaSnapshot idea
                    in track.IdeaSnapshots)
                {
                    Assert.That(
                        idea.SourceEntityId,
                        Is.EqualTo(
                            "ENTITY_MAYHEM_RUNTIME"
                        )
                    );
                }
            }
        }

        [Test]
        public void
            AuthoredDemoTapeCanReturnItsExactSourceRehearsalSet()
        {
            TextAsset asset =
                LoadAsset();

            DemoTape tape =
                AuthoredDemoTapeJsonLoader.Load(
                    asset,
                    "ENTITY_MAYHEM",
                    recordedTurn:
                    0,
                    out RehearsalSet rehearsalSet
                );

            Assert.That(
                rehearsalSet,
                Is.Not.Null
            );

            Assert.That(
                rehearsalSet.VhsSetId,
                Is.EqualTo(
                    tape.SourceSetId
                )
            );

            Assert.That(
                rehearsalSet.DisplayName,
                Is.EqualTo(
                    tape.SourceSetName
                )
            );

            Assert.That(
                rehearsalSet.VhsTracks.Count,
                Is.EqualTo(
                    tape.TrackSnapshots.Count
                )
            );

            for (int index = 0;
                 index < rehearsalSet.VhsTracks.Count;
                 index++)
            {
                Track sourceTrack =
                    rehearsalSet.VhsTracks[index];

                DemoTapeTrackSnapshot snapshot =
                    tape.TrackSnapshots[index];

                Assert.That(
                    sourceTrack.VhsTrackId,
                    Is.EqualTo(
                        snapshot.SourceTrackId
                    )
                );

                Assert.That(
                    sourceTrack.DisplayName,
                    Is.EqualTo(
                        snapshot.DisplayName
                    )
                );

                Assert.That(
                    sourceTrack.Ideas.Count,
                    Is.EqualTo(
                        snapshot.IdeaSnapshots.Count
                    )
                );
            }
        }
        
        [Test]
        public void
            NegativeTurnIsRejectedRatherThanModelledAsPrehistory()
        {
            TextAsset asset =
                LoadAsset();

            Assert.Throws<
                ArgumentOutOfRangeException>(
                () =>
                    AuthoredDemoTapeJsonLoader.Load(
                        asset,
                        "ENTITY_MAYHEM",
                        recordedTurn:
                            -1
                    )
            );
        }

        private static DemoTape LoadFreezingMoon(
            string sourceEntityId =
                "ENTITY_MAYHEM",
            int recordedTurn =
                0)
        {
            return AuthoredDemoTapeJsonLoader.Load(
                LoadAsset(),
                sourceEntityId,
                recordedTurn
            );
        }

        private static TextAsset LoadAsset()
        {
            TextAsset asset =
                AssetDatabase
                    .LoadAssetAtPath<TextAsset>(
                        FreezingMoonPath
                    );

            Assert.That(
                asset,
                Is.Not.Null,
                $"Missing authored DemoTape data at " +
                $"{FreezingMoonPath}"
            );

            return asset;
        }

        private static void AssertPair(
            DemoTapeIdeaSnapshot idea,
            TagAxis expectedAxis)
        {
            Assert.That(
                idea.PayloadType,
                Is.EqualTo(
                    IdeaPayloadType.TagPair
                )
            );

            Assert.That(
                idea.TagOccurrences.Count,
                Is.EqualTo(2)
            );

            DemoTapeTagOccurrenceSnapshot
                dominant =
                    idea.TagOccurrences[0];

            DemoTapeTagOccurrenceSnapshot
                submissive =
                    idea.TagOccurrences[1];

            Assert.That(
                dominant.Axis,
                Is.EqualTo(expectedAxis)
            );

            Assert.That(
                dominant.Pole,
                Is.EqualTo(
                    TagPole.Negative
                )
            );

            Assert.That(
                dominant.Degree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );

            Assert.That(
                submissive.Axis,
                Is.EqualTo(expectedAxis)
            );

            Assert.That(
                submissive.Pole,
                Is.EqualTo(
                    TagPole.Positive
                )
            );

            Assert.That(
                submissive.Degree,
                Is.EqualTo(
                    TagDegree.Weak
                )
            );
        }
    }
}