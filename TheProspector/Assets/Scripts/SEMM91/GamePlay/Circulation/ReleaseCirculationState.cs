using System.Collections.Generic;

namespace SEMM91.GamePlay.Circulation
{
    public class ReleaseCirculationState
    {
        public string SourceDemoTapeId { get; }
        public string SourceOwnerEntityId { get; }
        public int ReleasedTurn { get; }

        public int CirculationAgeTurns { get; private set; }

        public int Generation { get; private set; }
        public float Reach { get; private set; }
        public float Conveyance { get; private set; }
        public float Noise { get; private set; }
        public float Context { get; private set; }

        public bool ReachedHole { get; private set; }
        public bool ReachedTrustedTraders { get; private set; }
        public bool ReachedPeriphery { get; private set; }
        public bool LeakedToSociety { get; private set; }

        public ReleaseCirculationState(
            string sourceDemoTapeId,
            string sourceOwnerEntityId,
            int releasedTurn,
            float sourceConveyance)
        {
            SourceDemoTapeId = sourceDemoTapeId;
            SourceOwnerEntityId = sourceOwnerEntityId;
            ReleasedTurn = releasedTurn;

            CirculationAgeTurns = 0;

            Generation = 1;
            Reach = 0.10f;
            Conveyance = sourceConveyance;
            Noise = 0.10f;
            Context = 0.90f;

            ReachedHole = true;
        }

        public List<ReleaseCirculationEventType> Tick()
        {
            var emittedEvents = new List<ReleaseCirculationEventType>();

            CirculationAgeTurns++;

            ApplyGenerationByAge();
            ApplyGenerationValues();

            Reach = Clamp01(Reach + 0.20f);

            if (!ReachedTrustedTraders && Reach >= 0.20f)
            {
                ReachedTrustedTraders = true;
                emittedEvents.Add(ReleaseCirculationEventType.ReleaseReachedTrustedTraders);
            }

            if (!ReachedPeriphery && Reach >= 0.50f)
            {
                ReachedPeriphery = true;
                emittedEvents.Add(ReleaseCirculationEventType.ReleaseEnteredPeriphery);
            }

            if (!LeakedToSociety && Reach >= 0.80f)
            {
                LeakedToSociety = true;
                emittedEvents.Add(ReleaseCirculationEventType.ReleaseLeakedToSociety);
            }

            return emittedEvents;
        }

        private void ApplyGenerationByAge()
        {
            Generation = CirculationAgeTurns switch
            {
                0 => 1,
                1 => 2,
                2 => 3,
                _ => 4
            };
        }

        private void ApplyGenerationValues()
        {
            switch (Generation)
            {
                case 1:
                    Conveyance = Clamp01(Conveyance * 0.90f);
                    Noise = 0.10f;
                    Context = 0.90f;
                    break;

                case 2:
                    Conveyance = Clamp01(Conveyance * 0.70f);
                    Noise = 0.30f;
                    Context = 0.65f;
                    break;

                case 3:
                    Conveyance = Clamp01(Conveyance * 0.50f);
                    Noise = 0.55f;
                    Context = 0.40f;
                    break;

                default:
                    Conveyance = Clamp01(Conveyance * 0.30f);
                    Noise = 0.80f;
                    Context = 0.20f;
                    break;
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}