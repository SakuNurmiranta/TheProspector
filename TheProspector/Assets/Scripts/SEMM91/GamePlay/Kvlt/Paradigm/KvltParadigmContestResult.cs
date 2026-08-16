using System;
using System.Collections.Generic;
using SEMM91.GamePlay.Events;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public sealed class KvltParadigmContestResult
    {
        private readonly KvltPoserDeclaration[]
            newPoserDeclarations;

        public int SettledTurn { get; }
        public string HappeningId { get; }
        public string BehaviorOccurrenceId { get; }
        public string BehaviorTypeId { get; }
        public KvltParadigmOpposition Opposition { get; }
        public int FirstSideHailCount { get; }
        public int SecondSideHailCount { get; }
        public bool IsBeef { get; }
        public string WinningHailAspectId { get; }
        public string LosingHailAspectId { get; }
        public IReadOnlyList<KvltPoserDeclaration>
            NewPoserDeclarations => newPoserDeclarations;

        public bool HasClearWinner =>
            !IsBeef &&
            !string.IsNullOrWhiteSpace(WinningHailAspectId);

        // Entry-40 compatibility/readability aliases.
        public int HailCountA => FirstSideHailCount;
        public int HailCountB => SecondSideHailCount;
        public int StrongSideCount =>
            Math.Max(FirstSideHailCount, SecondSideHailCount);
        public int WeakSideCount =>
            Math.Min(FirstSideHailCount, SecondSideHailCount);
        public bool HasBeef => IsBeef;
        public bool IsParadigmBeef => IsBeef;
        public string WinnerHailAspectId => WinningHailAspectId;
        public string LoserHailAspectId => LosingHailAspectId;
        public IReadOnlyList<KvltPoserDeclaration>
            PoserDeclarations => NewPoserDeclarations;

        public KvltParadigmContestResult(
            int settledTurn,
            Happening happening,
            BehaviorOccurrence behavior,
            KvltParadigmOpposition opposition,
            int firstSideHailCount,
            int secondSideHailCount,
            bool isBeef,
            string winningHailAspectId,
            string losingHailAspectId,
            IReadOnlyList<KvltPoserDeclaration>
                sourceNewPoserDeclarations)
            : this(
                settledTurn,
                happening?.HappeningId,
                behavior,
                opposition,
                firstSideHailCount,
                secondSideHailCount,
                isBeef,
                winningHailAspectId,
                losingHailAspectId,
                sourceNewPoserDeclarations)
        {
            if (happening == null)
                throw new ArgumentNullException(nameof(happening));
        }

        public KvltParadigmContestResult(
            int settledTurn,
            string happeningId,
            BehaviorOccurrence behavior,
            KvltParadigmOpposition opposition,
            int firstSideHailCount,
            int secondSideHailCount,
            bool isBeef,
            string winningHailAspectId,
            string losingHailAspectId,
            IReadOnlyList<KvltPoserDeclaration>
                sourceNewPoserDeclarations)
        {
            if (settledTurn < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(settledTurn));

            if (firstSideHailCount <= 0 ||
                secondSideHailCount <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(firstSideHailCount));

            if (behavior == null)
                throw new ArgumentNullException(nameof(behavior));

            if (string.IsNullOrWhiteSpace(happeningId))
                throw new ArgumentException(
                    "Happening identity cannot be empty.",
                    nameof(happeningId));

            Opposition = opposition ??
                throw new ArgumentNullException(nameof(opposition));

            if (sourceNewPoserDeclarations == null)
                throw new ArgumentNullException(
                    nameof(sourceNewPoserDeclarations));

            if (isBeef &&
                sourceNewPoserDeclarations.Count != 0)
                throw new ArgumentException(
                    "Beef cannot create ordinary Poserdom.",
                    nameof(sourceNewPoserDeclarations));

            SettledTurn = settledTurn;
            HappeningId = happeningId.Trim();
            BehaviorOccurrenceId =
                behavior.BehaviorOccurrenceId;
            BehaviorTypeId = behavior.BehaviorTypeId;
            FirstSideHailCount = firstSideHailCount;
            SecondSideHailCount = secondSideHailCount;
            IsBeef = isBeef;
            WinningHailAspectId = winningHailAspectId ??
                string.Empty;
            LosingHailAspectId = losingHailAspectId ??
                string.Empty;

            newPoserDeclarations =
                new KvltPoserDeclaration[
                    sourceNewPoserDeclarations.Count];

            for (int i = 0;
                 i < sourceNewPoserDeclarations.Count;
                 i++)
            {
                newPoserDeclarations[i] =
                    sourceNewPoserDeclarations[i] ??
                    throw new ArgumentException(
                        "Contest result cannot contain null Poserdom.",
                        nameof(sourceNewPoserDeclarations));
            }
        }
    }
}
