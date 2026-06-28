using UnityEngine;
using UnityEngine.UI;
using static SEMM91.GameCoordinator;

namespace SEMM91.UI
{
    public sealed class SeasonalMapView : PersistentUIView
    {
        [Header("Presentation")]
        [SerializeField] private Image mapImage;

        [Header("Season sprites")]
        [SerializeField] private Sprite springSprite;
        [SerializeField] private Sprite summerSprite;
        [SerializeField] private Sprite fallSprite;
        [SerializeField] private Sprite winterSprite;

        private Season _displayedSeason;
        private bool _hasDisplayedSeason;

        public override void Refresh(UIContext context)
        {
            Season season = GameCoordinator.Instance.CurrentSeason;

            if (_hasDisplayedSeason && season == _displayedSeason)
                return;

            _displayedSeason = season;
            _hasDisplayedSeason = true;

            mapImage.sprite = GetSprite(season);
        }

        private Sprite GetSprite(Season season)
        {
            return season switch
            {
                Season.Spring => springSprite,
                Season.Summer => summerSprite,
                Season.Fall => fallSprite,
                Season.Winter => winterSprite,
                _ => springSprite
            };
        }
    }
}