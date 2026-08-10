using System;

namespace SEMM91.GamePlay.Kvlt.Transgression
{
    public sealed class AllegianceCrisisVote
    {
        public string VoterEntityId { get; }

        public AllegianceChoice Choice { get; }

        public int CastTurn { get; }

        public AllegianceCrisisVote(
            string voterEntityId,
            AllegianceChoice choice,
            int castTurn)
        {
            if (string.IsNullOrWhiteSpace(
                    voterEntityId))
            {
                throw new ArgumentException(
                    "Allegiance vote requires voter.",
                    nameof(voterEntityId)
                );
            }

            if (!Enum.IsDefined(
                    typeof(AllegianceChoice),
                    choice))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(choice)
                );
            }

            if (castTurn < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(castTurn)
                );
            }

            VoterEntityId =
                voterEntityId.Trim();

            Choice =
                choice;

            CastTurn =
                castTurn;
        }
    }
}