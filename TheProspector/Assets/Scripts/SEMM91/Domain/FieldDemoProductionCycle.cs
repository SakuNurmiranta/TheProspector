
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// One observed field-origin cultural product birth.
    /// </summary>
    public sealed class FieldDemoBirth
    {
        public int Day { get; }
        public string ProductId { get; }
        public string ArtistId { get; }
        public string InitialHolderNodeId { get; }

        internal FieldDemoBirth(
            int day,
            string productId,
            string artistId,
            string initialHolderNodeId)
        {
            Day = day;
            ProductId = productId;
            ArtistId = artistId;
            InitialHolderNodeId = initialHolderNodeId;
        }
    }

    /// <summary>
    /// Optional, deterministic field demo production.
    ///
    /// New products are created using the existing field
    /// and registered into the live circulation scenario.
    ///
    /// Does not create artist nodes, personal contacts,
    /// zine publications or independent postal activity.
    /// </summary>
    public sealed class FieldDemoProductionCycle : IDisposable
    {
        private readonly ConfigurableCirculationScenario _scenario;
        private readonly SixAxisProfileCatalog _aesthetics;
        private readonly Random _random;

        private readonly List<FieldDemoBirth> _births =
            new List<FieldDemoBirth>();

        private readonly IReadOnlyList<FieldDemoBirth> _birthView;

        private int _serial;
        private bool _disposed;

        public int IntervalDays { get; }

        public int BirthCount => _births.Count;

        public IReadOnlyList<FieldDemoBirth> Births => _birthView;

        public FieldDemoProductionCycle(
            ConfigurableCirculationScenario scenario,
            SixAxisProfileCatalog aesthetics,
            int intervalDays,
            int randomSeed)
        {
            _scenario = scenario ??
                throw new ArgumentNullException(nameof(scenario));

            _aesthetics = aesthetics ??
                throw new ArgumentNullException(nameof(aesthetics));

            if (intervalDays < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(intervalDays));

            IntervalDays = intervalDays;

            _random = new Random(
                unchecked(randomSeed ^ 0x4F3A921));

            _birthView = _births.AsReadOnly();

            _scenario.World.DayCompleted += OnDayCompleted;
        }

        private void OnDayCompleted(int day)
        {
            if (_disposed)
                return;

            if (day < 1 || day % IntervalDays != 0)
                return;

            _serial++;

            string suffix = _serial.ToString("D4");

            string demoId = "FIELD-DEMO-" + suffix;
            string artistId = "FIELD-ARTIST-" + suffix;

            // Reuse the scenario's complete registration
            // path for field-origin cultural products.
            var registration = _scenario.SpawnFieldDemo(
                demoId,
                artistId,
                _random);

            // The common A1-A6 profile type applies
            // equally to new demos and initial demos.
            var atmosphere =
                new SixAxisProfile(_random.Next(64));

            _aesthetics.AddProfile(
                SixAxisProfileRole.DemoAtmosphere,
                demoId,
                atmosphere);

            _births.Add(
                new FieldDemoBirth(
                    day,
                    demoId,
                    artistId,
                    registration.InitialHolderNodeId));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _scenario.World.DayCompleted -= OnDayCompleted;

            _disposed = true;
        }
    }
}
