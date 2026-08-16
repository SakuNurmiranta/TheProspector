using System;
using SEMM91.Core.Recordings;

namespace SEMM91.GamePlay.Circulation
{
    public sealed class SceneReleaseActivationSource
    {
        public SceneRelease Release { get; }

        public DemoTape DemoTape { get; }

        public SceneReleaseActivationSource(
            SceneRelease release,
            DemoTape demoTape)
        {
            Release =
                release ??
                throw new ArgumentNullException(
                    nameof(release)
                );

            DemoTape =
                demoTape ??
                throw new ArgumentNullException(
                    nameof(demoTape)
                );

            if (Release.SourceDemoTapeId !=
                DemoTape.DemoTapeId)
            {
                throw new ArgumentException(
                    "SceneRelease and DemoTape must " +
                    "refer to the same recording.",
                    nameof(demoTape)
                );
            }
        }
    }
}