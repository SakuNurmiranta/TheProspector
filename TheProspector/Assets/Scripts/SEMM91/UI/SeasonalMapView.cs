using SEMM91;
using UnityEngine;
using UnityEngine.UI;

namespace SEMM91.UI
{
    public sealed class SeasonalMapView : PersistentUIView
    {
        [SerializeField] private Image mapImage;

        [Header("Season sprites")]
        [SerializeField] private Sprite springSprite;
        [SerializeField] private Sprite summerSprite;
        [SerializeField] private Sprite fallSprite;
        [SerializeField] private Sprite winterSprite;

        private GameCoordinator.Season _displayedSeason;
        private bool _hasDisplayedSeason;
        private bool _hasWarnedMissingImage;

        public override void Refresh(UIContext context)
        {
            if (mapImage == null)
            {
                if (!_hasWarnedMissingImage)
                {
                    Debug.LogWarning("[SeasonalMapView] Map Image is not assigned.", this);
                    _hasWarnedMissingImage = true;
                }

                return;
            }

            GameCoordinator coordinator = GameCoordinator.Instance;

            // This is valid before the gameplay loop has started.
            if (coordinator == null)
                return;

            GameCoordinator.Season currentSeason = coordinator.CurrentSeason;

            if (_hasDisplayedSeason && currentSeason == _displayedSeason)
                return;

            Sprite sprite = GetSeasonSprite(currentSeason);

            if (sprite == null)
                return;

            _displayedSeason = currentSeason;
            _hasDisplayedSeason = true;
            mapImage.sprite = sprite;
        }

        private Sprite GetSeasonSprite(GameCoordinator.Season season)
        {
            return season switch
            {
                GameCoordinator.Season.Spring => springSprite,
                GameCoordinator.Season.Summer => summerSprite,
                GameCoordinator.Season.Fall => fallSprite,
                GameCoordinator.Season.Winter => winterSprite,
                _ => springSprite
            };
        }
    }
}