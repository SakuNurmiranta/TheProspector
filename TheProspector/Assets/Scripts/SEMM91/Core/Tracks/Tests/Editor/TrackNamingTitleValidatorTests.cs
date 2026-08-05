using NUnit.Framework;
using SEMM91.Core.Tracks;

namespace SEMM91.Tests.Editor.Tracks
{
    public sealed class
        TrackNamingTitleValidatorTests
    {
        [Test]
        public void TryValidate_OrdinaryPairTitle_ReturnsTrue()
        {
            bool valid =
                TrackNamingTitleValidator.TryValidate(
                    "Spitting on the Sacred Face",
                    out TrackNamingTitleValidationFailure
                        failure
                );

            Assert.That(valid, Is.True);

            Assert.That(
                failure,
                Is.EqualTo(
                    TrackNamingTitleValidationFailure.None
                )
            );
        }

        [Test]
        public void TryValidate_RepeatedContentWord_ReturnsFalse()
        {
            bool valid =
                TrackNamingTitleValidator.TryValidate(
                    "Sacred Sacred",
                    out TrackNamingTitleValidationFailure
                        failure
                );

            Assert.That(valid, Is.False);

            Assert.That(
                failure,
                Is.EqualTo(
                    TrackNamingTitleValidationFailure
                        .RepeatedContentWord
                )
            );
        }

        [Test]
        public void TryValidate_RepeatedContentWord_IsCaseInsensitive()
        {
            bool valid =
                TrackNamingTitleValidator.TryValidate(
                    "Void beneath the void",
                    out TrackNamingTitleValidationFailure
                        failure
                );

            Assert.That(valid, Is.False);

            Assert.That(
                failure,
                Is.EqualTo(
                    TrackNamingTitleValidationFailure
                        .RepeatedContentWord
                )
            );
        }

        [Test]
        public void TryValidate_RepeatedArticle_ReturnsFalse()
        {
            bool valid =
                TrackNamingTitleValidator.TryValidate(
                    "Desecration of the the Altar",
                    out TrackNamingTitleValidationFailure
                        failure
                );

            Assert.That(valid, Is.False);

            Assert.That(
                failure,
                Is.EqualTo(
                    TrackNamingTitleValidationFailure
                        .RepeatedFunctionWord
                )
            );
        }

        [Test]
        public void TryValidate_RepeatedFunctionSequence_ReturnsFalse()
        {
            bool valid =
                TrackNamingTitleValidator.TryValidate(
                    "Purpose within the within the Tomb",
                    out TrackNamingTitleValidationFailure
                        failure
                );

            Assert.That(valid, Is.False);

            Assert.That(
                failure,
                Is.EqualTo(
                    TrackNamingTitleValidationFailure
                        .RepeatedFunctionSequence
                )
            );
        }

        [Test]
        public void TryValidate_RepeatedNonAdjacentArticle_IsAllowed()
        {
            bool valid =
                TrackNamingTitleValidator.TryValidate(
                    "The Living Heart after the Final Word",
                    out TrackNamingTitleValidationFailure
                        failure
                );

            Assert.That(valid, Is.True);

            Assert.That(
                failure,
                Is.EqualTo(
                    TrackNamingTitleValidationFailure.None
                )
            );
        }

        [Test]
        public void TryValidate_ThirteenWords_ReturnsFalse()
        {
            const string title =
                "One Two Three Four Five Six Seven " +
                "Eight Nine Ten Eleven Twelve Thirteen";

            bool valid =
                TrackNamingTitleValidator.TryValidate(
                    title,
                    out TrackNamingTitleValidationFailure
                        failure
                );

            Assert.That(valid, Is.False);

            Assert.That(
                failure,
                Is.EqualTo(
                    TrackNamingTitleValidationFailure
                        .ExceedsMaximumWordCount
                )
            );
        }
    }
}