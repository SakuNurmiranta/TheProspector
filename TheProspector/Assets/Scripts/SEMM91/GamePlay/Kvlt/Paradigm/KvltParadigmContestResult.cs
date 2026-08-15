using System;
using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmContestResult
    {
        private readonly
            KvltPoserDeclaration[]
            newPoserDeclarations;

        public string HappeningId { get; }
        public string BehaviorOccurrenceId { get; }
        public string BehaviorTypeId { get; }

        public string FirstHailAspectId { get; }
        public string SecondHailAspectId { get; }

        public int FirstHailCount { get; }
        public int SecondHailCount { get; }

        public bool IsBeef { get; }

        public string WinningHailAspectId { get; }
        public string LosingHailAspectId { get; }

        public IReadOnlyList<KvltPoserDeclaration>
            NewPoserDeclarations =>
            newPoserDeclarations;

        internal KvltParadigmContestResult(
            string happeningId,
            string behaviorOccurrenceId,
            string behaviorTypeId,
            KvltParadigmOpposition opposition,
            int firstHailCount,
            int secondHailCount,
            bool isBeef,
            string winningHailAspectId,
            string losingHailAspectId,
            IReadOnlyList<KvltPoserDeclaration>
                sourceNewPoserDeclarations)
        {
            HappeningId = happeningId;
            BehaviorOccurrenceId =
                behaviorOccurrenceId;
            BehaviorTypeId = behaviorTypeId;

            FirstHailAspectId =
                opposition.FirstHailAspectId;

            SecondHailAspectId =
                opposition.SecondHailAspectId;

            FirstHailCount = firstHailCount;
            SecondHailCount = secondHailCount;

            IsBeef = isBeef;

            WinningHailAspectId =
                winningHailAspectId ??
                string.Empty;

            LosingHailAspectId =
                losingHailAspectId ??
                string.Empty;

            if (sourceNewPoserDeclarations == null)
            {
                throw new ArgumentNullException(
                    nameof(
                        sourceNewPoserDeclarations
                    )
                );
            }

            newPoserDeclarations =
                new KvltPoserDeclaration[
                    sourceNewPoserDeclarations.Count
                ];

            for (int i = 0;
                 i < newPoserDeclarations.Length;
                 i++)
            {
                newPoserDeclarations[i] =
                    sourceNewPoserDeclarations[i];
            }
        }
    }
}