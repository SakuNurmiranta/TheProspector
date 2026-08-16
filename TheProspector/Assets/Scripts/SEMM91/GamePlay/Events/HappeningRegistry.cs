using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Events
{
    /// <summary>
    /// Durable authoritative Happening history.
    ///
    /// Unlike WorldEventRegistry, Happening history
    /// is not automatically pruned.
    /// </summary>
    public sealed class HappeningRegistry
    {
        private readonly
            List<Happening>
                happeningsInOrder =
                    new();

        private readonly
            Dictionary<string, Happening>
                happeningsById =
                    new(
                        StringComparer.Ordinal
                    );

        public int Count =>
            happeningsInOrder.Count;

        public void Record(
            Happening happening)
        {
            if (happening == null)
            {
                throw new ArgumentNullException(
                    nameof(happening)
                );
            }

            if (happeningsById.ContainsKey(
                    happening.HappeningId))
            {
                throw new InvalidOperationException(
                    "Happening registry already contains " +
                    $"happening {happening.HappeningId}."
                );
            }

            happeningsInOrder.Add(
                happening
            );

            happeningsById.Add(
                happening.HappeningId,
                happening
            );
        }

        public bool TryGet(
            string happeningId,
            out Happening happening)
        {
            if (string.IsNullOrWhiteSpace(
                    happeningId))
            {
                happening = null;
                return false;
            }

            return happeningsById.TryGetValue(
                happeningId,
                out happening
            );
        }

        public IReadOnlyList<Happening>
            GetAll()
        {
            return happeningsInOrder.ToArray();
        }

        public IReadOnlyList<Happening>
            GetOwnedByCollective(
                string collectiveId)
        {
            if (string.IsNullOrWhiteSpace(
                    collectiveId))
            {
                return Array.Empty<Happening>();
            }

            List<Happening> matches =
                new();

            foreach (Happening happening
                     in happeningsInOrder)
            {
                if (happening.OwningCollectiveId ==
                    collectiveId)
                {
                    matches.Add(
                        happening
                    );
                }
            }

            return matches.ToArray();
        }

        public void ClearAll()
        {
            happeningsInOrder.Clear();
            happeningsById.Clear();
        }
    }
}