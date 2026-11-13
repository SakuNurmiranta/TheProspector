
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Common immutable A1-A6 bipolar profile.
    ///
    /// Bit 0 = A1, bit 1 = A2 ... bit 5 = A6.
    /// The profile itself has no owner or cultural role.
    ///
    /// Temporary boolean representation for Crucible 9.2.
    /// </summary>
    public sealed class SixAxisProfile
    {
        public const int AxisCount = 6;

        public int Mask { get; }

        public bool A1 => GetAxis(1);
        public bool A2 => GetAxis(2);
        public bool A3 => GetAxis(3);
        public bool A4 => GetAxis(4);
        public bool A5 => GetAxis(5);
        public bool A6 => GetAxis(6);

        public SixAxisProfile(int mask)
        {
            if (mask < 0 || mask > 63)
                throw new ArgumentOutOfRangeException(
                    nameof(mask));

            Mask = mask;
        }

        public bool GetAxis(int axis)
        {
            ValidateAxis(axis);
            return (Mask & (1 << (axis - 1))) != 0;
        }

        public int MatchingAxes(SixAxisProfile other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            int differences = Mask ^ other.Mask;
            int matches = AxisCount;

            while (differences != 0)
            {
                matches -= differences & 1;
                differences >>= 1;
            }

            return matches;
        }

        internal static void ValidateAxis(int axis)
        {
            if (axis < 1 || axis > AxisCount)
                throw new ArgumentOutOfRangeException(
                    nameof(axis));
        }
    }

    /// <summary>
    /// The semantic role is stored outside SixAxisProfile.
    ///
    /// Personal preference, product atmosphere, editorial ideal,
    /// and an editorial description of a person are not the
    /// same claim, even when they use the same data structure.
    /// </summary>
    public enum SixAxisProfileRole
    {
        PersonalPreference,
        DemoAtmosphere,
        EditorialPreference,
        PublishedContactDescription,
        PublishedDemoDescription
    }

    /// <summary>
    /// Immutable composition of several aesthetic profiles.
    ///
    /// A composition preserves positive/negative axis counts.
    /// It does NOT force a collection of different contents into
    /// one binary ideal and does not calculate legitimacy.
    /// </summary>
    public sealed class SixAxisComposition
    {
        private readonly int[] _positiveCounts =
            new int[SixAxisProfile.AxisCount];

        public int SourceCount { get; }

        public SixAxisComposition(
            IEnumerable<SixAxisProfile> sources)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            int count = 0;

            foreach (var profile in sources)
            {
                if (profile == null)
                    throw new ArgumentException(
                        "Composition cannot contain null profiles.",
                        nameof(sources));

                count++;

                for (int axis = 1;
                     axis <= SixAxisProfile.AxisCount;
                     axis++)
                {
                    if (profile.GetAxis(axis))
                        _positiveCounts[axis - 1]++;
                }
            }

            if (count == 0)
                throw new ArgumentException(
                    "Composition requires at least one source.",
                    nameof(sources));

            SourceCount = count;
        }

        public int GetPositiveCount(int axis)
        {
            SixAxisProfile.ValidateAxis(axis);
            return _positiveCounts[axis - 1];
        }

        public double GetPositiveShare(int axis)
        {
            return GetPositiveCount(axis) /
                   (double)SourceCount;
        }
    }

    /// <summary>
    /// Optional aesthetic metadata for a simulation.
    ///
    /// Stores shared SixAxisProfile values in different
    /// semantic roles, and content compositions separately.
    ///
    /// None of Network, Node, CulturalProduct, or postal
    /// circulation requires this registry to exist.
    /// </summary>
    public sealed class SixAxisProfileCatalog
    {
        private readonly Dictionary<
            (SixAxisProfileRole Role, string EntityId, string IssueId),
            SixAxisProfile> _profiles =
                new Dictionary<
                    (SixAxisProfileRole, string, string),
                    SixAxisProfile>();

        private readonly Dictionary<string, SixAxisComposition>
            _compositions =
                new Dictionary<string, SixAxisComposition>(
                    StringComparer.Ordinal);

        public void AddProfile(
            SixAxisProfileRole role,
            string entityId,
            SixAxisProfile profile,
            string issueProductId = null)
        {
            ValidateKey(role, entityId, issueProductId);

            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var key = (role, entityId, issueProductId);

            if (_profiles.ContainsKey(key))
                throw new InvalidOperationException(
                    "Profile already exists in this role and scope.");

            _profiles.Add(key, profile);
        }

        public SixAxisProfile GetProfile(
            SixAxisProfileRole role,
            string entityId,
            string issueProductId = null)
        {
            ValidateKey(role, entityId, issueProductId);

            if (!_profiles.TryGetValue(
                    (role, entityId, issueProductId),
                    out var profile))
            {
                throw new InvalidOperationException(
                    "Aesthetic profile has not been initialized.");
            }

            return profile;
        }

        public bool TryGetProfile(
            SixAxisProfileRole role,
            string entityId,
            out SixAxisProfile profile,
            string issueProductId = null)
        {
            ValidateKey(role, entityId, issueProductId);

            return _profiles.TryGetValue(
                (role, entityId, issueProductId),
                out profile);
        }

        public void AddComposition(
            string contentId,
            SixAxisComposition composition)
        {
            ValidateId(contentId, nameof(contentId));

            if (composition == null)
                throw new ArgumentNullException(
                    nameof(composition));

            if (_compositions.ContainsKey(contentId))
                throw new InvalidOperationException(
                    "Composition already exists.");

            _compositions.Add(contentId, composition);
        }

        public SixAxisComposition GetComposition(
            string contentId)
        {
            ValidateId(contentId, nameof(contentId));

            if (!_compositions.TryGetValue(
                    contentId, out var composition))
            {
                throw new InvalidOperationException(
                    "Composition has not been initialized.");
            }

            return composition;
        }

        /// <summary>
        /// Create initial aesthetic metadata for an existing
        /// simulation world.
        ///
        /// Personal and demo profiles use the same profile
        /// type. The editor's ideal is a separate profile.
        ///
        /// Contact descriptions may differ from the person's
        /// private preference, and are scoped to a particular
        /// immutable published issue.
        ///
        /// The first issue composition is derived from the
        /// seven advertised demo profiles, NOT the editor's
        /// personal or editorial preference.
        /// </summary>
        public static SixAxisProfileCatalog CreateInitialized(
            IEnumerable<string> nodeIds,
            IEnumerable<string> demoIds,
            string editorNodeId,
            IEnumerable<string> listedNodeIds,
            IReadOnlyDictionary<string, string> advertisedDemoIds,
            string issueProductId,
            int randomSeed,
            double descriptionErrorProbability = 0.25)
        {
            if (nodeIds == null)
                throw new ArgumentNullException(nameof(nodeIds));

            if (demoIds == null)
                throw new ArgumentNullException(nameof(demoIds));

            if (listedNodeIds == null)
                throw new ArgumentNullException(nameof(listedNodeIds));

            if (advertisedDemoIds == null)
                throw new ArgumentNullException(
                    nameof(advertisedDemoIds));

            ValidateId(editorNodeId, nameof(editorNodeId));
            ValidateId(issueProductId, nameof(issueProductId));

            if (double.IsNaN(descriptionErrorProbability) ||
                descriptionErrorProbability < 0.0 ||
                descriptionErrorProbability > 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(descriptionErrorProbability));
            }

            var catalog = new SixAxisProfileCatalog();

            var random = new Random(
                unchecked(randomSeed ^ 0x39A271));

            var people = new List<string>(nodeIds);
            people.Sort(StringComparer.Ordinal);

            foreach (string id in people)
            {
                catalog.AddProfile(
                    SixAxisProfileRole.PersonalPreference,
                    id,
                    new SixAxisProfile(random.Next(64)));
            }

            var demos = new List<string>(demoIds);
            demos.Sort(StringComparer.Ordinal);

            foreach (string id in demos)
            {
                catalog.AddProfile(
                    SixAxisProfileRole.DemoAtmosphere,
                    id,
                    new SixAxisProfile(random.Next(64)));
            }

            // Editorial intention is not the editor's
            // private preference, even when both happen
            // to receive the same numerical profile.
            catalog.GetProfile(
                SixAxisProfileRole.PersonalPreference,
                editorNodeId);

            catalog.AddProfile(
                SixAxisProfileRole.EditorialPreference,
                editorNodeId,
                new SixAxisProfile(random.Next(64)));

            var listed = new List<string>(listedNodeIds);
            listed.Sort(StringComparer.Ordinal);

            var issueContents = new List<SixAxisProfile>();
            
            // Independent random stream: adding published demo
            // descriptions must not change existing personal,
            // demo or editorial profile initialization.
            var advertisementRandom = new Random(
                unchecked(randomSeed ^ 0x58C1FA));

            foreach (string id in listed)
            {
                var actualPerson = catalog.GetProfile(
                    SixAxisProfileRole.PersonalPreference,
                    id);

                int descriptionMask = actualPerson.Mask;

                for (int axis = 0;
                     axis < SixAxisProfile.AxisCount;
                     axis++)
                {
                    if (random.NextDouble() <
                        descriptionErrorProbability)
                    {
                        descriptionMask ^= 1 << axis;
                    }
                }

                catalog.AddProfile(
                    SixAxisProfileRole.PublishedContactDescription,
                    id,
                    new SixAxisProfile(descriptionMask),
                    issueProductId);

                if (!advertisedDemoIds.TryGetValue(
                        id, out string demoId))
                {
                    throw new ArgumentException(
                        "Listed person has no advertised demo.",
                        nameof(advertisedDemoIds));
                }

                var actualDemo = catalog.GetProfile(
                    SixAxisProfileRole.DemoAtmosphere,
                    demoId);

                issueContents.Add(actualDemo);

// The advertisement describes the demo as published
// by the editor. It is not the demo's true profile.
                int advertisedMask = actualDemo.Mask;

                for (int axis = 0;
                     axis < SixAxisProfile.AxisCount;
                     axis++)
                {
                    if (advertisementRandom.NextDouble() <
                        descriptionErrorProbability)
                    {
                        advertisedMask ^= 1 << axis;
                    }
                }

                catalog.AddProfile(
                    SixAxisProfileRole.PublishedDemoDescription,
                    demoId,
                    new SixAxisProfile(advertisedMask),
                    issueProductId);
            }

            catalog.AddComposition(
                issueProductId,
                new SixAxisComposition(issueContents));

            return catalog;
        }

        private static void ValidateKey(
            SixAxisProfileRole role,
            string entityId,
            string issueProductId)
        {
            if (!Enum.IsDefined(typeof(SixAxisProfileRole), role))
                throw new ArgumentOutOfRangeException(
                    nameof(role));

            ValidateId(entityId, nameof(entityId));

            if (role ==
                SixAxisProfileRole.PublishedContactDescription ||
                role ==
                SixAxisProfileRole.PublishedDemoDescription)
            {
                ValidateId(
                    issueProductId, nameof(issueProductId));
            }
            else if (issueProductId != null)
            {
                throw new ArgumentException(
                    "This profile role is not issue-scoped.",
                    nameof(issueProductId));
            }
        }

        private static void ValidateId(
            string id,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException(
                    "Identifier is required.",
                    parameterName);
        }
    }
}
