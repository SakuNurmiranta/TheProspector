using System;

namespace SEMM91.Domain
{
    /// <summary>
    /// Kytkee fyysisen kasettilevityksen
    /// olemassa olevaan kentän SIR-seurantaan.
    ///
    /// Ei luo uusia kopioita tai lähetyksiä.
    /// </summary>
    public sealed class FieldDemoCopyDispatchObserver
        : IDisposable
    {
        private readonly PositiveCopyDistributionService
            _distribution;

        private readonly FieldDemoCopyWave _wave;

        private bool _disposed;

        public FieldDemoCopyDispatchObserver(
            PositiveCopyDistributionService distribution,
            FieldDemoCopyWave wave)
        {
            _distribution = distribution ??
                            throw new ArgumentNullException(
                                nameof(distribution));

            _wave = wave ??
                    throw new ArgumentNullException(
                        nameof(wave));

            _distribution.CopyDispatched += OnCopyDispatched;
        }

        private void OnCopyDispatched(
            CopyDistributionShipment shipment)
        {
            if (_disposed)
                return;

            // Suljettua SIR-aaltoa ei avata.
            // Jo onnistunutta fyysistä postitusta
            // ei saa kumota seurannan vuoksi.
            if (_wave.IsPassivelyDiscoverable)
                return;

            // Tämä käsittelee vain kenttään
            // ja kulttuurituotteeseen kuuluvat
            // oikeat kopiointilähetykset.
            _wave.ObserveCopyDispatch(shipment);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _distribution.CopyDispatched -= OnCopyDispatched;

            _disposed = true;
        }
    }
}