using System;
using NUnit.Framework;
using SEMM91.Core.Tags;

namespace SEMM91.GamePlay.Actions.Tests
{
    public class IdeaExpansionTests
    {
        [Test]
        public void CreateIdea_DefaultsToConviction()
        {
            var payload = new DraftedActionPayload(
                DraftedActionType.CreateIdea,
                3
            );

            Assert.That(
                payload.IdeaSourceContainerType,
                Is.EqualTo(TagContainerType.Conviction)
            );
        }
        
        [Test]
        public void CreateIdea_PreservesExplicitSource()
        {
            var payload = new DraftedActionPayload(
                DraftedActionType.CreateIdea,
                3,
                TagContainerType.Transient
            );

            Assert.That(
                payload.IdeaSourceContainerType,
                Is.EqualTo(TagContainerType.Transient)
            );
        }
        
        [Test]
        public void NonIdeaPayload_HasNoIdeaSource()
        {
            var payload = new DraftedActionPayload(
                DraftedActionType.RehearseActiveSet,
                3
            );

            Assert.That(
                payload.IdeaSourceContainerType,
                Is.Null
            );
        }
        
        [Test]
        public void NonIdeaPayload_RejectsIdeaSource()
        {
            Assert.Throws<ArgumentException>(
                () => new DraftedActionPayload(
                    DraftedActionType.RehearseActiveSet,
                    3,
                    TagContainerType.Mood
                )
            );
        }
    }
}