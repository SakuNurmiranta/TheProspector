using System;
using System.Collections.Generic;
using SEMM91.Core.Tags;
using SEMM91.GamePlay.Actions.History;

namespace SEMM91.GamePlay.Events
{
    /// <summary>
    /// One signed semantic signal carried by a factual world event.
    ///
    /// This is not yet a Questing contribution. Character-specific
    /// interpretation belongs to a contribution provider.
    /// </summary>
    public sealed class WorldEventAxisSignal
    {
        public TagAxis Axis { get; }
        public float SignedIntensity { get; }
        public string Description { get; }

        public WorldEventAxisSignal(
            TagAxis axis,
            float signedIntensity,
            string description)
        {
            if (!Enum.IsDefined(typeof(TagAxis), axis))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(axis),
                    axis,
                    "World-event signal requires a valid tag axis."
                );
            }

            if (float.IsNaN(signedIntensity) ||
                float.IsInfinity(signedIntensity))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(signedIntensity),
                    signedIntensity,
                    "World-event signal intensity must be finite."
                );
            }

            Axis = axis;
            SignedIntensity = signedIntensity;
            Description = description ?? string.Empty;
        }
    }

    /// <summary>
    /// Factual authoritative record of a world consequence,
    /// reaction, or state-changing occurrence.
    /// </summary>
    public sealed class WorldEventRecord
    {
        public string EventId { get; }
        public int GlobalTurn { get; }
        public string AffectedEntityId { get; }

        public CharacterActionKey? SourceActionKey { get; }

        public IReadOnlyList<WorldEventAxisSignal>
            AxisSignals { get; }

        public WorldEventRecord(
            string eventId,
            int globalTurn,
            string affectedEntityId,
            CharacterActionKey? sourceActionKey,
            IEnumerable<WorldEventAxisSignal> axisSignals)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                throw new ArgumentException(
                    "World event ID is required.",
                    nameof(eventId)
                );
            }

            if (globalTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(globalTurn),
                    globalTurn,
                    "Global turn cannot be negative."
                );
            }

            if (string.IsNullOrWhiteSpace(affectedEntityId))
            {
                throw new ArgumentException(
                    "Affected entity ID is required.",
                    nameof(affectedEntityId)
                );
            }

            if (axisSignals == null)
            {
                throw new ArgumentNullException(
                    nameof(axisSignals)
                );
            }

            List<WorldEventAxisSignal> signalList = new();

            foreach (WorldEventAxisSignal signal in axisSignals)
            {
                if (signal == null)
                {
                    throw new ArgumentException(
                        "World-event signal collection contains " +
                        "a null entry.",
                        nameof(axisSignals)
                    );
                }

                signalList.Add(signal);
            }

            EventId = eventId;
            GlobalTurn = globalTurn;
            AffectedEntityId = affectedEntityId;
            SourceActionKey = sourceActionKey;
            AxisSignals = signalList.ToArray();
        }
    }
}