using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    /// <summary>
    /// Domain-only initialization values. Public fields keep this object
    /// compatible with a later Unity Inspector wrapper without UnityEngine.
    /// </summary>
    [Serializable]
    public sealed class CirculationInitializationParameters
    {
        public int CountryAPopulation = 30;
        public int CountryBPopulation = 70;
        public int AddressSlotsPerCountry = 100;
        public int FriendshipParticipationPercent = 40;
        public int MinimumCircleSize = 2;
        public int MaximumCircleSize = 5;
        public int ZineListedContacts = 7;
        public int AdditionalUnlistedDemoOwners = 8;
        public int RandomSeed = 921;
    }

    /// <summary>
    /// Initial world data plus explicit editorial/ownership metadata.
    /// Creating this plan does not give any node knowledge of a zine issue.
    /// </summary>
    public sealed class CirculationInitialWorld
    {
        public CirculationWorldDefinition Definition { get; }
        public string EditorNodeId { get; }
        public IReadOnlyList<string> ZineListedNodeIds { get; }
        public IReadOnlyList<string> UnlistedDemoOwnerIds { get; }
        public IReadOnlyList<IReadOnlyList<string>> FriendCircles { get; }

        internal CirculationInitialWorld(
            CirculationWorldDefinition definition,
            string editorNodeId,
            List<string> listed,
            List<string> unlisted,
            List<IReadOnlyList<string>> circles)
        {
            Definition = definition;
            EditorNodeId = editorNodeId;
            ZineListedNodeIds = listed.AsReadOnly();
            UnlistedDemoOwnerIds = unlisted.AsReadOnly();
            FriendCircles = circles.AsReadOnly();
        }
    }

    /// <summary>
    /// Reproducible initial population, address slots, small friendship
    /// components, and unique cassette holders. Zine distribution, public
    /// profiles, and future field spawning are intentionally separate stages.
    /// </summary>
    public static class CirculationInitialWorldFactory
    {
        public static CirculationInitialWorld Create(
            CirculationInitializationParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            Validate(parameters);

            var random = new Random(parameters.RandomSeed);
            var definition = new CirculationWorldDefinition();
            var aNodes = AddPeople(definition, "A", parameters.CountryAPopulation);
            var bNodes = AddPeople(definition, "B", parameters.CountryBPopulation);

            PlacePeople(definition, "A", aNodes,
                parameters.AddressSlotsPerCountry, random);
            PlacePeople(definition, "B", bNodes,
                parameters.AddressSlotsPerCountry, random);

            var circles = new List<IReadOnlyList<string>>();
            AddFriendCircles(definition, aNodes, parameters, random, circles);
            AddFriendCircles(definition, bNodes, parameters, random, circles);

            // The editor is one of B's existing people, not an extra node.
            var editor = bNodes[random.Next(bNodes.Count)];
            var listed = new List<string> { editor };
            var listedSet = new HashSet<string>(listed, StringComparer.Ordinal);

            // Prefer actual existing friends of the editor. Remaining entries
            // are editorial contacts; they do NOT create interpersonal arcs.
            foreach (var edge in definition.Connections)
            {
                if (edge.SenderId == editor &&
                    listedSet.Add(edge.RecipientId))
                {
                    listed.Add(edge.RecipientId);
                    if (listed.Count == parameters.ZineListedContacts)
                        break;
                }
            }

            var remainingB = new List<string>(bNodes);
            Shuffle(remainingB, random);
            foreach (var node in remainingB)
            {
                if (listed.Count == parameters.ZineListedContacts)
                    break;
                if (listedSet.Add(node))
                    listed.Add(node);
            }

            int extras = parameters.AdditionalUnlistedDemoOwners;
            int desiredA = (int)Math.Round(
                extras * (double)parameters.CountryAPopulation /
                (parameters.CountryAPopulation + parameters.CountryBPopulation),
                MidpointRounding.AwayFromZero);

            int minA = Math.Max(0, extras -
                (parameters.CountryBPopulation - listed.Count));
            int maxA = Math.Min(extras, parameters.CountryAPopulation);
            int countA = Math.Max(minA, Math.Min(maxA, desiredA));
            int countB = extras - countA;

            var aCandidates = new List<string>(aNodes);
            Shuffle(aCandidates, random);
            var bCandidates = bNodes.Where(id => !listedSet.Contains(id)).ToList();
            Shuffle(bCandidates, random);

            var unlisted = new List<string>();
            unlisted.AddRange(aCandidates.Take(countA));
            unlisted.AddRange(bCandidates.Take(countB));

            int number = 0;
            foreach (var holder in listed.Concat(unlisted))
            {
                number++;
                definition.AddSeed(holder, "D" + number.ToString("000"));
            }

            return new CirculationInitialWorld(
                definition, editor, listed, unlisted, circles);
        }

        private static void Validate(CirculationInitializationParameters p)
        {
            if (p.CountryAPopulation < 1 || p.CountryBPopulation < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(p), "Both countries must have people.");

            if (p.AddressSlotsPerCountry < Math.Max(
                    p.CountryAPopulation, p.CountryBPopulation))
                throw new ArgumentOutOfRangeException(
                    nameof(p), "Not enough address slots.");

            if (p.FriendshipParticipationPercent < 0 ||
                p.FriendshipParticipationPercent > 100)
                throw new ArgumentOutOfRangeException(
                    nameof(p), "Invalid friendship percentage.");

            if (p.MinimumCircleSize < 2 ||
                p.MaximumCircleSize < p.MinimumCircleSize)
                throw new ArgumentOutOfRangeException(
                    nameof(p), "Invalid friendship-circle size.");

            if (p.ZineListedContacts < 1 ||
                p.ZineListedContacts > p.CountryBPopulation)
                throw new ArgumentOutOfRangeException(
                    nameof(p), "Zine contacts must be B residents.");

            int maxExtras = p.CountryAPopulation +
                p.CountryBPopulation - p.ZineListedContacts;
            if (p.AdditionalUnlistedDemoOwners < 0 ||
                p.AdditionalUnlistedDemoOwners > maxExtras)
                throw new ArgumentOutOfRangeException(
                    nameof(p), "Invalid number of unlisted cassette holders.");

            ValidateParticipantCount(p.CountryAPopulation, p);
            ValidateParticipantCount(p.CountryBPopulation, p);
        }

        private static void ValidateParticipantCount(
            int population, CirculationInitializationParameters p)
        {
            int count = (int)Math.Round(
                population * p.FriendshipParticipationPercent / 100.0,
                MidpointRounding.AwayFromZero);

            if (count != 0 && (count < p.MinimumCircleSize ||
                !CanPartition(count, p.MinimumCircleSize, p.MaximumCircleSize)))
                throw new ArgumentException(
                    "Friendship participants cannot form valid circles.");
        }

        private static bool CanPartition(int remaining, int min, int max)
        {
            if (remaining == 0)
                return true;
            for (int size = min; size <= max && size <= remaining; size++)
                if (remaining == size ||
                    (remaining > size && CanPartition(remaining - size, min, max)))
                    return true;
            return false;
        }

        private static List<string> AddPeople(
            CirculationWorldDefinition definition, string country, int count)
        {
            var result = new List<string>();
            for (int i = 1; i <= count; i++)
            {
                var id = country + i.ToString("000");
                definition.AddNode(id, country);
                result.Add(id);
            }
            return result;
        }

        private static void PlacePeople(
            CirculationWorldDefinition definition,
            string country,
            List<string> people,
            int slots,
            Random random)
        {
            var available = new List<string>();
            for (int i = 1; i <= slots; i++)
            {
                string addressId = "ADDR-" + country + "-" + i.ToString("000");
                definition.AddAddress(addressId, country);
                available.Add(addressId);
            }
            Shuffle(available, random);
            for (int i = 0; i < people.Count; i++)
                definition.PlaceNode(people[i], available[i]);
        }

        private static void AddFriendCircles(
            CirculationWorldDefinition definition,
            List<string> people,
            CirculationInitializationParameters p,
            Random random,
            List<IReadOnlyList<string>> circles)
        {
            int participants = (int)Math.Round(
                people.Count * p.FriendshipParticipationPercent / 100.0,
                MidpointRounding.AwayFromZero);
            var shuffled = new List<string>(people);
            Shuffle(shuffled, random);

            int position = 0;
            while (position < participants)
            {
                int remaining = participants - position;
                var sizes = new List<int>();
                for (int size = p.MinimumCircleSize;
                     size <= p.MaximumCircleSize && size <= remaining;
                     size++)
                {
                    if (CanPartition(remaining - size,
                            p.MinimumCircleSize, p.MaximumCircleSize))
                        sizes.Add(size);
                }

                int sizeToUse = sizes[random.Next(sizes.Count)];
                var circle = shuffled.GetRange(position, sizeToUse);
                circles.Add(circle.AsReadOnly());

                // A connected friendship circle, not a complete graph.
                // Every friendship has two directed interpersonal arcs.
                for (int i = 1; i < circle.Count; i++)
                {
                    AddFriendship(definition, circle[i - 1], circle[i]);
                }
                position += sizeToUse;
            }
        }

        private static void AddFriendship(
            CirculationWorldDefinition definition, string a, string b)
        {
            definition.AddConnection("FRIEND:" + a + ":" + b, a, b);
            definition.AddConnection("FRIEND:" + b + ":" + a, b, a);
            definition.MarkKnownMissing(a, b);
            definition.MarkKnownMissing(b, a);
        }

        private static void Shuffle<T>(IList<T> values, Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T tmp = values[i];
                values[i] = values[j];
                values[j] = tmp;
            }
        }
    }
}
