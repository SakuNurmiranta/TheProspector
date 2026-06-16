using NUnit.Framework;
using SEMM91.GamePlay.Actions.History;

namespace SEMM91.GamePlay.Actions.Tests
{
    public class CharacterActionHistoryRegistryTests
    {
        private CharacterActionHistoryRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registry =
                new CharacterActionHistoryRegistry();
        }

        [Test]
        public void Record_StoresResolvedCharacterAction()
        {
            CharacterActionRecord record =
                CreateRecord(
                    characterId: "Leader_0",
                    globalTurn: 2,
                    actionType:
                        DraftedActionType.CreateIdea,
                    actionPosition: 1
                );

            registry.Record(record);

            var records =
                registry.GetRecent("Leader_0");

            Assert.That(records.Count, Is.EqualTo(1));

            Assert.That(
                records[0].ActionType,
                Is.EqualTo(
                    DraftedActionType.CreateIdea
                )
            );

            Assert.That(
                records[0].ActionKey.GlobalTurn,
                Is.EqualTo(2)
            );
        }

        [Test]
        public void Record_KeepsCharactersSeparated()
        {
            registry.Record(CreateRecord(
                "Leader_0",
                1,
                DraftedActionType.CreateIdea,
                1
            ));

            registry.Record(CreateRecord(
                "Leader_1",
                1,
                DraftedActionType.Rest,
                1
            ));

            Assert.That(
                registry.GetRecent("Leader_0").Count,
                Is.EqualTo(1)
            );

            Assert.That(
                registry.GetRecent("Leader_1").Count,
                Is.EqualTo(1)
            );

            Assert.That(
                registry.GetRecent("Leader_0")[0]
                    .ActionType,
                Is.EqualTo(
                    DraftedActionType.CreateIdea
                )
            );

            Assert.That(
                registry.GetRecent("Leader_1")[0]
                    .ActionType,
                Is.EqualTo(
                    DraftedActionType.Rest
                )
            );
        }

        [Test]
        public void Record_RetainsOnlyLatestFourGlobalTurns()
        {
            for (int turn = 0; turn <= 5; turn++)
            {
                registry.Record(CreateRecord(
                    "Leader_0",
                    turn,
                    DraftedActionType.CreateIdea,
                    1
                ));
            }

            var records =
                registry.GetRecent("Leader_0");

            Assert.That(records.Count, Is.EqualTo(4));

            Assert.That(
                records[0].ActionKey.GlobalTurn,
                Is.EqualTo(2)
            );

            Assert.That(
                records[3].ActionKey.GlobalTurn,
                Is.EqualTo(5)
            );
        }

        [Test]
        public void Record_PreservesMultipleActionsFromSameTurn()
        {
            registry.Record(CreateRecord(
                "Leader_0",
                3,
                DraftedActionType.CreateIdea,
                1
            ));

            registry.Record(CreateRecord(
                "Leader_0",
                3,
                DraftedActionType.CreateIdea,
                2
            ));

            registry.Record(CreateRecord(
                "Leader_0",
                3,
                DraftedActionType.Rest,
                3
            ));

            var records =
                registry.GetRecent("Leader_0");

            Assert.That(records.Count, Is.EqualTo(3));
            Assert.That(records[2].WasThirdAction, Is.True);
        }

        [Test]
        public void LinkResultingEvent_AttachesCausalReference()
        {
            CharacterActionRecord record =
                CreateRecord(
                    "Leader_0",
                    3,
                    DraftedActionType
                        .ReleaseLatestDemoToKvlt,
                    1
                );

            registry.Record(record);

            bool linked =
                registry.LinkResultingEvent(
                    record.ActionKey,
                    "SceneEvent_14"
                );

            var records =
                registry.GetRecent("Leader_0");

            Assert.That(linked, Is.True);

            Assert.That(
                records[0].ResultingEventIds,
                Has.Count.EqualTo(1)
            );

            Assert.That(
                records[0].ResultingEventIds[0],
                Is.EqualTo("SceneEvent_14")
            );
        }

        [Test]
        public void LinkResultingEvent_DoesNotAttachDuplicate()
        {
            CharacterActionRecord record =
                CreateRecord(
                    "Leader_0",
                    3,
                    DraftedActionType
                        .ReleaseLatestDemoToKvlt,
                    1
                );

            registry.Record(record);

            bool firstLink =
                registry.LinkResultingEvent(
                    record.ActionKey,
                    "SceneEvent_14"
                );

            bool duplicateLink =
                registry.LinkResultingEvent(
                    record.ActionKey,
                    "SceneEvent_14"
                );

            Assert.That(firstLink, Is.True);
            Assert.That(duplicateLink, Is.False);

            Assert.That(
                registry.GetRecent("Leader_0")[0]
                    .ResultingEventIds,
                Has.Count.EqualTo(1)
            );
        }

        [Test]
        public void Record_DuplicateActionKey_Throws()
        {
            CharacterActionRecord first =
                CreateRecord(
                    "Leader_0",
                    2,
                    DraftedActionType.CreateIdea,
                    1
                );

            CharacterActionRecord duplicate =
                CreateRecord(
                    "Leader_0",
                    2,
                    DraftedActionType.Rest,
                    1
                );

            registry.Record(first);

            Assert.Throws<
                System.InvalidOperationException>(
                () => registry.Record(duplicate)
            );
        }

        [Test]
        public void GetRecent_UnknownCharacter_ReturnsEmpty()
        {
            var records =
                registry.GetRecent("Unknown");

            Assert.That(records, Is.Empty);
        }

        private static CharacterActionRecord CreateRecord(
            string characterId,
            int globalTurn,
            DraftedActionType actionType,
            int actionPosition)
        {
            CharacterActionKey actionKey =
                new CharacterActionKey(
                    characterId,
                    globalTurn,
                    actionPosition
                );

            return new CharacterActionRecord(
                actionKey: actionKey,
                clientId: 0,
                roundIndex: globalTurn / 4,
                actionType: actionType,
                wasSuccessful: null
            );
        }
    }
}