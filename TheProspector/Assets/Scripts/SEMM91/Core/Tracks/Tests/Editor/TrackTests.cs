using NUnit.Framework;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class TrackTests
    {
        [Test]
        public void TrySetGeneratedDisplayName_ValidName_UpdatesTitle()
        {
            Track track = new(
                "TRACK_TEST",
                "Untitled Track",
                0.0f,
                0
            );

            bool changed =
                track.TrySetGeneratedDisplayName(
                    "Consecrated Spit"
                );

            Assert.That(changed, Is.True);

            Assert.That(
                track.DisplayName,
                Is.EqualTo("Consecrated Spit")
            );
        }

        [Test]
        public void TrySetGeneratedDisplayName_WhitespaceAroundName_TrimsTitle()
        {
            Track track = new(
                "TRACK_TEST",
                "Untitled Track",
                0.0f,
                0
            );

            bool changed =
                track.TrySetGeneratedDisplayName(
                    "  Consecrated Spit  "
                );

            Assert.That(changed, Is.True);

            Assert.That(
                track.DisplayName,
                Is.EqualTo("Consecrated Spit")
            );
        }

        [Test]
        public void TrySetGeneratedDisplayName_SameName_DoesNotReportChange()
        {
            Track track = new(
                "TRACK_TEST",
                "Consecrated Spit",
                0.0f,
                0
            );

            bool changed =
                track.TrySetGeneratedDisplayName(
                    "Consecrated Spit"
                );

            Assert.That(changed, Is.False);

            Assert.That(
                track.DisplayName,
                Is.EqualTo("Consecrated Spit")
            );
        }

        [Test]
        public void TrySetGeneratedDisplayName_WhitespaceOnly_PreservesExistingTitle()
        {
            Track track = new(
                "TRACK_TEST",
                "Untitled Track",
                0.0f,
                0
            );

            bool changed =
                track.TrySetGeneratedDisplayName("   ");

            Assert.That(changed, Is.False);

            Assert.That(
                track.DisplayName,
                Is.EqualTo("Untitled Track")
            );
        }
    }
}