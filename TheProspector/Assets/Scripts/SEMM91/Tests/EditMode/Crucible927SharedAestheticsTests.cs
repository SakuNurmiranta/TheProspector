
using System;
using System.Linq;
using NUnit.Framework;
using SEMM91.Domain;

public sealed class Crucible927SharedAestheticsTests
{
    private static DemoSession Create(int seed = 921)
    {
        return DemoSession.CreateConfigured(
            new CirculationInitializationParameters
            {
                RandomSeed = seed
            });
    }

    private static string ADistributor(DemoSession session)
    {
        return session.InitialZine.DistributorNodeIds
            .Single(id => id.StartsWith("A"));
    }

    private static ZineSixAxisContactEligibility
        WithDescriptions(
            DemoSession session,
            string reader,
            int readerMask,
            string target,
            int targetMask)
    {
        var catalog = new SixAxisProfileCatalog();

        catalog.AddProfile(
            SixAxisProfileRole.PersonalPreference,
            reader,
            new SixAxisProfile(readerMask));

        foreach (string id in
                 session.InitialWorld.ZineListedNodeIds)
        {
            catalog.AddProfile(
                SixAxisProfileRole.PublishedContactDescription,
                id,
                new SixAxisProfile(
                    id == target ? targetMask : 63),
                session.InitialZine.Issue.ProductId);
        }

        return new ZineSixAxisContactEligibility(
            session.Scenario.Network,
            session.InitialZine.Knowledge,
            session.InitialZine.Issue,
            catalog);
    }

    [Test]
    public void SharedProfile_UsesSixAxesAndExactMatching()
    {
        var zero = new SixAxisProfile(0);

        Assert.AreEqual(
            6, zero.MatchingAxes(new SixAxisProfile(0)));

        Assert.AreEqual(
            4, zero.MatchingAxes(new SixAxisProfile(3)));

        Assert.AreEqual(
            3, zero.MatchingAxes(new SixAxisProfile(7)));

        Assert.AreEqual(
            0, zero.MatchingAxes(new SixAxisProfile(63)));

        Assert.IsFalse(zero.GetAxis(1));
        Assert.IsTrue(new SixAxisProfile(32).GetAxis(6));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new SixAxisProfile(64));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => zero.GetAxis(0));
    }

    [Test]
    public void Catalog_ReusesSameProfileTypeAcrossRoles()
    {
        var catalog = new SixAxisProfileCatalog();

        catalog.AddProfile(
            SixAxisProfileRole.PersonalPreference,
            "N1",
            new SixAxisProfile(1));

        catalog.AddProfile(
            SixAxisProfileRole.DemoAtmosphere,
            "D1",
            new SixAxisProfile(2));

        catalog.AddProfile(
            SixAxisProfileRole.EditorialPreference,
            "N1",
            new SixAxisProfile(4));

        catalog.AddProfile(
            SixAxisProfileRole.PublishedContactDescription,
            "N1",
            new SixAxisProfile(8),
            "ISSUE1");

        Assert.AreEqual(
            1,
            catalog.GetProfile(
                SixAxisProfileRole.PersonalPreference,
                "N1").Mask);

        Assert.AreEqual(
            2,
            catalog.GetProfile(
                SixAxisProfileRole.DemoAtmosphere,
                "D1").Mask);

        Assert.AreEqual(
            4,
            catalog.GetProfile(
                SixAxisProfileRole.EditorialPreference,
                "N1").Mask);

        Assert.AreEqual(
            8,
            catalog.GetProfile(
                SixAxisProfileRole.PublishedContactDescription,
                "N1",
                "ISSUE1").Mask);

        Assert.Throws<InvalidOperationException>(() =>
            catalog.AddProfile(
                SixAxisProfileRole.PersonalPreference,
                "N1",
                new SixAxisProfile(63)));

        Assert.Throws<ArgumentException>(() =>
            catalog.AddProfile(
                SixAxisProfileRole.PublishedContactDescription,
                "N2",
                new SixAxisProfile(0)));
    }

    [Test]
    public void Composition_PreservesAxisDistribution()
    {
        var composition = new SixAxisComposition(
            new[]
            {
                new SixAxisProfile(0),
                new SixAxisProfile(1),
                new SixAxisProfile(3),
                new SixAxisProfile(63)
            });

        Assert.AreEqual(4, composition.SourceCount);
        Assert.AreEqual(3, composition.GetPositiveCount(1));
        Assert.AreEqual(2, composition.GetPositiveCount(2));
        Assert.AreEqual(1, composition.GetPositiveCount(3));
        Assert.AreEqual(0.75,
            composition.GetPositiveShare(1), 0.000001);
        Assert.AreEqual(0.5,
            composition.GetPositiveShare(2), 0.000001);

        Assert.Throws<ArgumentException>(() =>
            new SixAxisComposition(
                Array.Empty<SixAxisProfile>()));
    }

    [Test]
    public void ConfiguredWorld_AssignsProfilesToAllPeopleAndDemos()
    {
        var session = Create();
        var catalog = session.AestheticProfiles;

        foreach (string id in session.Scenario.NodeIds)
        {
            Assert.IsNotNull(catalog.GetProfile(
                SixAxisProfileRole.PersonalPreference, id));
        }

        foreach (string id in session.Scenario.ProductIds)
        {
            Assert.IsNotNull(catalog.GetProfile(
                SixAxisProfileRole.DemoAtmosphere, id));
        }

        Assert.IsNotNull(catalog.GetProfile(
            SixAxisProfileRole.EditorialPreference,
            session.InitialWorld.EditorNodeId));
    }

    [Test]
    public void IssueComposition_IsDerivedFromAdvertisedDemos()
    {
        var session = Create();
        var catalog = session.AestheticProfiles;
        var zine = session.InitialZine;

        var composition =
            catalog.GetComposition(zine.Issue.ProductId);

        Assert.AreEqual(7, composition.SourceCount);

        for (int axis = 1; axis <= 6; axis++)
        {
            int expected = zine.AdvertisedDemoIds.Values
                .Count(demoId =>
                    catalog.GetProfile(
                        SixAxisProfileRole.DemoAtmosphere,
                        demoId).GetAxis(axis));

            Assert.AreEqual(
                expected,
                composition.GetPositiveCount(axis));
        }

        // Editorial intention remains a separate profile,
        // not an alias for the resulting composition.
        Assert.IsNotNull(catalog.GetProfile(
            SixAxisProfileRole.EditorialPreference,
            zine.Issue.EditorNodeId));
    }

    [Test]
    public void DayZero_UnawareReaderHasNoContactCandidates()
    {
        var session = Create();
        string reader = ADistributor(session);

        Assert.IsFalse(
            session.InitialZine.Knowledge.HasReceived(
                reader, session.InitialZine.Issue.ProductId));

        Assert.AreEqual(
            0,
            session.SixAxisContacts
                .GetCandidates(reader).Count);
    }

    [Test]
    public void FourMatches_AcceptsPublishedContact()
    {
        var session = Create();
        session.AdvanceDay();

        string reader = ADistributor(session);
        string target =
            session.InitialWorld.ZineListedNodeIds[0];

        var policy = WithDescriptions(
            session, reader, 0, target, 3);

        var candidates = policy.GetCandidates(reader);

        Assert.AreEqual(1, candidates.Count);
        Assert.AreEqual(
            target, candidates[0].TargetNodeId);
        Assert.AreEqual(
            4, candidates[0].MatchingAxes);

        Assert.AreEqual(
            session.InitialZine.Knowledge
                .GetLatestPublishedAddress(reader, target),
            candidates[0].PublishedAddressId);
    }

    [Test]
    public void ThreeMatches_RejectsPublishedContact()
    {
        var session = Create();
        session.AdvanceDay();

        string reader = ADistributor(session);
        string target =
            session.InitialWorld.ZineListedNodeIds[0];

        var policy = WithDescriptions(
            session, reader, 0, target, 7);

        Assert.AreEqual(
            0, policy.GetCandidates(reader).Count);
    }

    [Test]
    public void ExistingPersonalConnection_ExcludesCandidate()
    {
        var session = Create();
        session.AdvanceDay();

        string reader = ADistributor(session);
        string target =
            session.InitialWorld.ZineListedNodeIds[0];

        var policy = WithDescriptions(
            session, reader, 0, target, 0);

        Assert.AreEqual(1, policy.GetCandidates(reader).Count);

        session.Scenario.Network.AddConnection(
            "TEST-927-KNOWN",
            reader,
            target,
            CommunicationLayer.Interpersonal,
            "personal");

        Assert.AreEqual(0, policy.GetCandidates(reader).Count);
    }

    [Test]
    public void SameSeed_ReproducesAllProfileContexts()
    {
        var a = Create(12345);
        var b = Create(12345);

        foreach (string id in a.Scenario.NodeIds)
        {
            Assert.AreEqual(
                a.AestheticProfiles.GetProfile(
                    SixAxisProfileRole.PersonalPreference, id).Mask,
                b.AestheticProfiles.GetProfile(
                    SixAxisProfileRole.PersonalPreference, id).Mask);
        }

        foreach (string id in a.Scenario.ProductIds)
        {
            Assert.AreEqual(
                a.AestheticProfiles.GetProfile(
                    SixAxisProfileRole.DemoAtmosphere, id).Mask,
                b.AestheticProfiles.GetProfile(
                    SixAxisProfileRole.DemoAtmosphere, id).Mask);
        }

        foreach (string id in a.InitialWorld.ZineListedNodeIds)
        {
            Assert.AreEqual(
                a.SixAxisContacts.GetEditorialDescription(id).Mask,
                b.SixAxisContacts.GetEditorialDescription(id).Mask);
        }

        for (int axis = 1; axis <= 6; axis++)
        {
            Assert.AreEqual(
                a.AestheticProfiles.GetComposition(
                    a.InitialZine.Issue.ProductId)
                    .GetPositiveCount(axis),
                b.AestheticProfiles.GetComposition(
                    b.InitialZine.Issue.ProductId)
                    .GetPositiveCount(axis));
        }
    }

    [Test]
    public void Eligibility_DoesNotSendMailOrCreateArcs()
    {
        var session = Create();
        session.AdvanceDay();

        string reader = ADistributor(session);

        int before = session.Scenario.NodeIds.Sum(id =>
            session.Scenario.Network.GetOutgoingConnections(
                id, CommunicationLayer.Interpersonal).Count);

        int pending = session.Scenario.Clock.PendingCount;

        var candidates =
            session.SixAxisContacts.GetCandidates(reader);

        Assert.IsTrue(candidates.All(candidate =>
            session.InitialWorld.ZineListedNodeIds.Contains(
                candidate.TargetNodeId)));

        int after = session.Scenario.NodeIds.Sum(id =>
            session.Scenario.Network.GetOutgoingConnections(
                id, CommunicationLayer.Interpersonal).Count);

        Assert.AreEqual(before, after);
        Assert.AreEqual(
            pending, session.Scenario.Clock.PendingCount);
    }

    [Test]
    public void LegacySession_DoesNotRequireAestheticProfiles()
    {
        var session = DemoSession.CreateDefault();

        Assert.IsNull(session.AestheticProfiles);
        Assert.IsNull(session.SixAxisContacts);

        Assert.DoesNotThrow(() => session.AdvanceDay());
    }
}
