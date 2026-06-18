using NUnit.Framework;

namespace SEMM91.Core.Tags.Tests.Editor
{
   
    public class HeldTagTests
    {
        [Test]
        public void StableTag_DoesNotEvaporateAtBoundary()
        {
            HeldTag heldTag = new HeldTag(
                new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                )
            );

            bool shouldEvaporate =
                heldTag.TickTurnBoundaryAndShouldEvaporate();

            Assert.That(shouldEvaporate, Is.False);
            Assert.That(heldTag.RemainingTurns, Is.EqualTo(0));
        }
        
        [Test]
        public void OneTurnUnstableTag_EvaporatesAtFirstBoundary()
        {
            HeldTag heldTag = new HeldTag(
                new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                )
            );

            heldTag.MarkUnstable(1, "test");

            bool shouldEvaporate =
                heldTag.TickTurnBoundaryAndShouldEvaporate();

            Assert.That(shouldEvaporate, Is.True);
            Assert.That(heldTag.RemainingTurns, Is.EqualTo(0));
        }
        
        [Test]
        public void TwoTurnUnstableTag_SurvivesOneBoundary()
        {
            HeldTag heldTag = new HeldTag(
                new TagInstance(
                    TagAxis.Symbolic,
                    TagPole.Positive,
                    TagDegree.Weak
                )
            );

            heldTag.MarkUnstable(2, "test");

            Assert.That(
                heldTag.TickTurnBoundaryAndShouldEvaporate(),
                Is.False
            );

            Assert.That(heldTag.RemainingTurns, Is.EqualTo(1));

            Assert.That(
                heldTag.TickTurnBoundaryAndShouldEvaporate(),
                Is.True
            );

            Assert.That(heldTag.RemainingTurns, Is.EqualTo(0));
        }
    }
}