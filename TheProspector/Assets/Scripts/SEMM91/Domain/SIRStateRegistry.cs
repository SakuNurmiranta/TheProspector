
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class SIRStateRegistry
    {
        /*
         * TODO: Field circulation reach.
         *
         * Track cumulative demo reach within the field
         * area associated with each explicit node.
         *
         * Node SIR activity and cumulative field reach
         * must remain separate.
         *
         * Reach must not disappear when a node
         * transitions from I to R.
         *
         * Later decisions:
         * - Effects of copy and transfer events.
         * - Aggregation of local field reach.
         * - Overlapping field areas.
         * - Relationship to MaxFieldReach.
         *
         * No numerical reach calculations yet.
         */

        private readonly Dictionary<(string, string), SIRState>
            _nodeStates =
                new Dictionary<(string, string), SIRState>();

        private readonly Dictionary<(string, string), SIRState>
            _fieldStates =
                new Dictionary<(string, string), SIRState>();

        public SIRState GetNodeState(
            Node node,
            CulturalProduct product)
        {
            return Read(
                _nodeStates,
                NodeKey(node, product));
        }

        public SIRState GetFieldState(
            Field field,
            CulturalProduct product)
        {
            return Read(
                _fieldStates,
                FieldKey(field, product));
        }

        public void BeginNodeCopying(
            Node node,
            CulturalProduct product)
        {
            Begin(
                _nodeStates,
                NodeKey(node, product));
        }

        public void EndNodeCopying(
            Node node,
            CulturalProduct product)
        {
            End(
                _nodeStates,
                NodeKey(node, product));
        }

        public void BeginFieldWave(
            Field field,
            CulturalProduct product)
        {
            Begin(
                _fieldStates,
                FieldKey(field, product));
        }

        public void CloseFieldWave(
            Field field,
            CulturalProduct product)
        {
            End(
                _fieldStates,
                FieldKey(field, product));
        }

        private static SIRState Read(
            Dictionary<(string, string), SIRState> states,
            (string, string) key)
        {
            return states.TryGetValue(key, out var state)
                ? state
                : SIRState.S;
        }

        private static void Begin(
            Dictionary<(string, string), SIRState> states,
            (string, string) key)
        {
            if (Read(states, key) == SIRState.R)
                throw new InvalidOperationException(
                    "A completed SIR cycle cannot restart.");

            states[key] = SIRState.I;
        }

        private static void End(
            Dictionary<(string, string), SIRState> states,
            (string, string) key)
        {
            if (Read(states, key) != SIRState.I)
                throw new InvalidOperationException(
                    "Only an active SIR cycle can end.");

            states[key] = SIRState.R;
        }

        private static (string, string) NodeKey(
            Node node,
            CulturalProduct product)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            return (node.Id, product.Id);
        }

        private static (string, string) FieldKey(
            Field field,
            CulturalProduct product)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            if (product == null)
                throw new ArgumentNullException(nameof(product));

            return (field.Id, product.Id);
        }
    }
}
