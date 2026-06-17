using System;
using SEMM91.GamePlay.Entities;
using SEMM91.GamePlay.Gestation.Questing;


namespace SEMM91.GamePlay.Gestation
{
    //Kajmadalthas Pajazzo
    public sealed class PajazzoResolution
    {
        public QuestingMoodInput MoodInput { get; }
        
        public QuestingCompositeBuildResult Composite { get; }
        
        public QuestingResult Projection { get; }

        internal PajazzoResolution(
            QuestingMoodInput moodInput,
            QuestingCompositeBuildResult composite,
            QuestingResult projection)
        {
            MoodInput = moodInput ??
                        throw new ArgumentNullException(nameof(moodInput));
            Composite = composite ??
                        throw new ArgumentNullException(nameof(composite));
            Projection = projection ??
                        throw new ArgumentNullException(nameof(projection));
        }
    }
    
    /// <summary>
    /// Kajmadalthas Pajazzo.
    ///
    /// Coordinates authoritative Mood reading, composite assembly and projection without game state meddling.
    /// </summary>
    public sealed class TheWhiteSlotMachine
    {
        private readonly QuestingMoodInputReader 
            _moodInputReader;
        
        private readonly QuestingCompositeAssembler 
            _compositeAssembler;
        
        private readonly QuestingProjectionResolver 
            _projectionResolver;

        public TheWhiteSlotMachine(
            QuestingMoodInputReader moodInputReader,
            QuestingCompositeAssembler compositeAssembler,
            QuestingProjectionResolver projectionResolver)
        {
            _moodInputReader = moodInputReader ??
                               throw new ArgumentNullException(
                                   nameof(moodInputReader)
                               );
            
            _compositeAssembler = compositeAssembler ??
                                  throw new ArgumentNullException(
                                      nameof(compositeAssembler)
                                  );
            
            _projectionResolver = projectionResolver ??
                                  throw new ArgumentNullException(
                                      nameof(projectionResolver)
                                  );
        }

        public bool TryResolve(
            GameEntity character,
            int currentGlobalTurn,
            float sceneSynchronisation,
            out PajazzoResolution resolution,
            out string failureReason)
        {
            resolution = null;
            failureReason = string.Empty;

            if (!_moodInputReader.TryRead(
                    character,
                    out QuestingMoodInput moodInput,
                    out failureReason))
            {
                return false;
            }

            QuestingCompositeContext context =
                new QuestingCompositeContext(
                    characterId: character.EntityId,
                    activeAxis: moodInput.ActiveAxis,
                    currentGlobalTurn: currentGlobalTurn
                );

            QuestingCompositeBuildResult composite =
                _compositeAssembler.Build(context);

            QuestingRequest request =
                new QuestingRequest(
                    activeAxis: moodInput.ActiveAxis,
                    moodValue: moodInput.SignedValue,
                    compositeValue: composite.CompositeValue,
                    sceneSynchronisation: sceneSynchronisation
                );
            
            QuestingResult projection = 
                _projectionResolver.Resolve(request);

            resolution = new PajazzoResolution(
                moodInput,
                composite,
                projection
            );

            return true;
        }
    }
}