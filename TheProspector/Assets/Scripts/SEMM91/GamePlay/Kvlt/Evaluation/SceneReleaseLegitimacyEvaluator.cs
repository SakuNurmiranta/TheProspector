using System;
using System.Collections.Generic;
using SEMM91.Core.Recordings;
using SEMM91.GamePlay.Circulation;

namespace SEMM91.GamePlay.Kvlt.Evaluation
{
    public sealed class SceneReleaseLegitimacyEvaluator
    {
        private readonly
            TrackLegitimacyEvaluator
                trackEvaluator =
                    new();

        public SceneReleaseLegitimacyEvaluation Evaluate(
            SceneRelease release,
            DemoTape demoTape,
            TrackEvaluationEnvironment environment,
            IReadOnlyList<
                TrackActivationEvaluationSnapshot>
                trackActivations)
        {
            if (release == null)
            {
                throw new ArgumentNullException(
                    nameof(release)
                );
            }

            if (demoTape == null)
            {
                throw new ArgumentNullException(
                    nameof(demoTape)
                );
            }

            if (environment == null)
            {
                throw new ArgumentNullException(
                    nameof(environment)
                );
            }

            if (trackActivations == null)
            {
                throw new ArgumentNullException(
                    nameof(trackActivations)
                );
            }

            if (release.SourceDemoTapeId !=
                demoTape.DemoTapeId)
            {
                throw new ArgumentException(
                    "SceneRelease and DemoTape " +
                    "identity do not match.",
                    nameof(demoTape)
                );
            }

            Dictionary<
                string,
                TrackActivationEvaluationSnapshot>
                activationByTrackId =
                    BuildActivationLookup(
                        release,
                        demoTape,
                        trackActivations
                    );

            List<TrackLegitimacyEvaluation>
                trackEvaluations =
                    new();

            foreach (DemoTapeTrackSnapshot track
                     in demoTape.TrackSnapshots)
            {
                TrackActivationEvaluationSnapshot
                    activation;

                if (!activationByTrackId.TryGetValue(
                        track.SourceTrackId,
                        out activation))
                {
                    activation =
                        new TrackActivationEvaluationSnapshot(
                            release.ReleaseId,
                            demoTape.DemoTapeId,
                            track.SourceTrackId,
                            Array.Empty<
                                IdeaActivationEvaluationSnapshot>()
                        );
                }

                trackEvaluations.Add(
                    trackEvaluator.Evaluate(
                        track,
                        environment,
                        activation
                    )
                );
            }

            return new SceneReleaseLegitimacyEvaluation(
                release.ReleaseId,
                demoTape.DemoTapeId,
                release.SourceOwnerEntityId,
                environment.SettledTurn,
                trackEvaluations
            );
        }

        private static Dictionary<
            string,
            TrackActivationEvaluationSnapshot>
            BuildActivationLookup(
                SceneRelease release,
                DemoTape demoTape,
                IReadOnlyList<
                    TrackActivationEvaluationSnapshot>
                    trackActivations)
        {
            HashSet<string> validTrackIds =
                new();

            foreach (DemoTapeTrackSnapshot track
                     in demoTape.TrackSnapshots)
            {
                validTrackIds.Add(
                    track.SourceTrackId
                );
            }

            Dictionary<
                string,
                TrackActivationEvaluationSnapshot>
                result =
                    new();

            foreach (
                TrackActivationEvaluationSnapshot activation
                in trackActivations)
            {
                if (activation == null)
                {
                    throw new ArgumentException(
                        "Release activation input " +
                        "cannot contain null.",
                        nameof(trackActivations)
                    );
                }

                if (activation.SourceReleaseId !=
                    release.ReleaseId)
                {
                    throw new ArgumentException(
                        "Track activation belongs to " +
                        "a different SceneRelease.",
                        nameof(trackActivations)
                    );
                }

                if (activation.SourceDemoTapeId !=
                    demoTape.DemoTapeId)
                {
                    throw new ArgumentException(
                        "Track activation belongs to " +
                        "a different DemoTape.",
                        nameof(trackActivations)
                    );
                }

                if (!validTrackIds.Contains(
                        activation.SourceTrackId))
                {
                    throw new ArgumentException(
                        "Track activation references " +
                        "a Track not present on DemoTape | " +
                        $"track={activation.SourceTrackId}",
                        nameof(trackActivations)
                    );
                }

                if (!result.TryAdd(
                        activation.SourceTrackId,
                        activation))
                {
                    throw new ArgumentException(
                        "Release activation contains " +
                        "duplicate Track input | " +
                        $"track={activation.SourceTrackId}",
                        nameof(trackActivations)
                    );
                }
            }

            return result;
        }
    }
}