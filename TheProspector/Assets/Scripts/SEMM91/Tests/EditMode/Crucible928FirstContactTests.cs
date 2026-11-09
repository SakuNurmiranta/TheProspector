
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible928FirstContactTests
{
    private sealed class Setup
    {
        public readonly DemoSession Session = DemoSession.CreateConfigured(
            new CirculationInitializationParameters { RandomSeed = 921 });

        public readonly string Sender;
        public readonly string Recipient;
        public readonly string ProductId;
        public readonly ZineFirstContactPostalService Postal;

        public Setup()
        {
            var initial = Session.InitialWorld;

            // A and B cannot share an initial friendship.
            Sender = initial.UnlistedDemoOwnerIds
                .First(id => id.StartsWith("A"));

            Recipient = initial.ZineListedNodeIds
                .First(id => id.StartsWith("B"));

            ProductId = initial.Definition.Seeds
                .Single(s => s.SourceNodeId == Sender).DemoId;

            Session.InitialZine.Knowledge.ReceiveIssue(
                Sender, Session.InitialZine.Issue);

            var catalog = new SixAxisProfileCatalog();

            catalog.AddProfile(
                SixAxisProfileRole.PersonalPreference,
                Sender,
                new SixAxisProfile(0));

            foreach (var id in initial.ZineListedNodeIds)
            {
                catalog.AddProfile(
                    SixAxisProfileRole.PublishedContactDescription,
                    id,
                    new SixAxisProfile(
                        id == Recipient ? 3 : 63),
                    Session.InitialZine.Issue.ProductId);
            }

            var eligible = new ZineSixAxisContactEligibility(
                Session.Scenario.Network,
                Session.InitialZine.Knowledge,
                Session.InitialZine.Issue,
                catalog);

            Postal = new ZineFirstContactPostalService(
                Session.Scenario.Network,
                Session.Scenario.Clock,
                new ZineContactOutreachPolicy(
                    Session.Scenario.Network,
                    Session.InitialZine.Knowledge),
                Session.Scenario.Exchange,
                (sender, target) => eligible.GetCandidates(sender)
                    .Any(c => c.TargetNodeId == target));
        }

        public PostalShipment Send(string id = "FIRST-TEST")
        {

            var original = Session.Scenario.GetOriginal(ProductId);

            var copy = Session.Scenario.Inventory.RecordCopy(
                original.Id,
                "TEST-INTRO-COPY:" + id,
                Sender,
                Sender);


            Assert.IsTrue(Postal.TrySend(
                Sender, false, Recipient, copy, id,
                out var shipment));

            return shipment;
        }

        public BacklogItem Open()
        {
            Session.Scenario.Clock.AdvanceDays(
                PostalTimingRules.InternationalDays);

            BacklogItem request = null;

            while (Session.Scenario.Exchange.TryOpenNext(
                       Recipient, out _, out var item))
            {
                if (item.Package.Id == "FIRST-TEST")
                    request = item;
            }

            Assert.IsNotNull(request);
            return request;
        }
    }

    [Test]
    public void QualifiedFirstContact_UsesSixAxesInsteadOfLegacyBoolean()
    {
        var t = new Setup();

        Assert.AreEqual(
            4, t.Session.SixAxisContacts.MinimumMatchingAxes);

        var shipment = t.Send();

        Assert.AreEqual(
            PostalShipmentStatus.InTransit, shipment.Status);

        Assert.IsTrue(
            t.Postal.HasPendingOutreach(t.Sender));

        Assert.IsFalse(
            t.Session.Scenario.Network.GetOutgoingConnections(
                t.Sender, CommunicationLayer.Interpersonal)
                .Any(c => c.TargetNodeId == t.Recipient));
    }

    [Test]
    public void NoResponse_DoesNotCreatePersonalArc()
    {
        var t = new Setup();
        t.Send();

        t.Session.Scenario.Clock.AdvanceDays(30);

        Assert.IsFalse(
            t.Postal.HasPendingOutreach(t.Sender));

        Assert.IsFalse(
            t.Session.Scenario.Network.GetOutgoingConnections(
                t.Sender, CommunicationLayer.Interpersonal)
                .Any(c => c.TargetNodeId == t.Recipient));
    }

    [Test]
    public void NegativeReply_CreatesTwoArcsOnlyAfterDelivery()
    {
        var t = new Setup();
        t.Send();

        var item = t.Open();

        var reply = t.Postal.ReplyWithAddress(
            item,
            "NEGATIVE-REPLY",
            feedbackProduct:
                t.Session.Scenario.GetDemo(t.ProductId),
            thumb: -1);

        Assert.AreEqual(-1, reply.Package.Thumb);

        Assert.IsFalse(
            t.Session.Scenario.Network.GetOutgoingConnections(
                t.Sender, CommunicationLayer.Interpersonal)
                .Any(c => c.TargetNodeId == t.Recipient));

        t.Session.Scenario.Clock.AdvanceDays(
            PostalTimingRules.InternationalDays);

        Assert.IsTrue(
            t.Session.Scenario.Network.GetOutgoingConnections(
                t.Sender, CommunicationLayer.Interpersonal)
                .Any(c => c.TargetNodeId == t.Recipient));

        Assert.IsTrue(
            t.Session.Scenario.Network.GetOutgoingConnections(
                t.Recipient, CommunicationLayer.Interpersonal)
                .Any(c => c.TargetNodeId == t.Sender));
    }

    [Test]
    public void PositiveReply_CanIncludeExistingCassetteCopy()
    {
        var t = new Setup();
        t.Send();

        var item = t.Open();

       
        var recipientDemoId = t.Session.InitialWorld.Definition.Seeds
            .Single(s => s.SourceNodeId == t.Recipient).DemoId;

        var original = t.Session.Scenario.GetOriginal(recipientDemoId);

        var replyCopy = t.Session.Scenario.Inventory.RecordCopy(
            original.Id,
            "TEST-REPLY-COPY",
            t.Recipient,
            t.Recipient);


        var reply = t.Postal.ReplyWithAddress(
            item,
            "POSITIVE-REPLY",
            replyCopy,
            t.Session.Scenario.GetDemo(t.ProductId),
            +1);

        Assert.AreEqual(
            replyCopy.Id, reply.Package.CassetteCopyId);

        Assert.AreEqual(+1, reply.Package.Thumb);
        Assert.AreEqual(
            PostalShipmentStatus.InTransit, reply.Status);
    }

    [Test]
    public void GetHeldCopies_ExcludesTransitAndSortsDeterministically()
    {
        var t = new Setup();
        var inventory = t.Session.Scenario.Inventory;

        var copies = inventory.GetHeldCopies(t.Sender);
        Assert.IsTrue(copies.Count >= 1);

        CollectionAssert.AreEqual(
            copies.Select(c => c.Id)
                .OrderBy(x => x).ToArray(),
            copies.Select(c => c.Id).ToArray());

        var sent = t.Send();

        Assert.IsFalse(inventory.GetHeldCopies(t.Sender)
            .Any(c => c.Id == sent.Package.CassetteCopyId));
    }

    [Test]
    public void ConfiguredSession_HasAutonomousSixAxisContactCycle()
    {
        var session = DemoSession.CreateConfigured(
            new CirculationInitializationParameters());

        Assert.IsNotNull(session.FirstContactPostal);
        Assert.IsNotNull(session.FirstContactCycle);
        Assert.AreEqual(0, session.FirstContactCycle.SentCount);

        session.AdvanceDay();

        // Day 1 is reserved for initial bulk distribution.
        Assert.AreEqual(0, session.FirstContactCycle.SentCount);
        Assert.AreEqual(100, session.NodeCount);
    }

    [Test]
    public void KnownZineAndPhysicalDemo_EnableAutonomousFirstLetters()
    {
        var session = DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = 921
            });

        // Test setup provides issue knowledge, not a postal
        // shipment or an interpersonal network connection.
        foreach (var seed in session.InitialWorld.Definition.Seeds)
        {
            session.InitialZine.Knowledge.ReceiveIssue(
                seed.SourceNodeId,
                session.InitialZine.Issue);
        }

        session.AdvanceDays(2);

        Assert.Greater(
            session.FirstContactCycle.SentCount, 0);
    }
}
