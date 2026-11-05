
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Kerää toimitussolmulle osoitetuista,
    /// avatuista kirjeistä julkaistavaa tietoa.
    ///
    /// Ei lue muiden solmujen yksityistä tilaa.
    /// Ei muuta viestejä eikä skeptisyysrekisteriä.
    /// </summary>
    public sealed class ZineEditorialMailCollector : IDisposable
    {
        private readonly ZineEditorialCycle _editorial;
        private readonly TimedPostalExchange _exchange;

        private readonly HashSet<string> _capturedIds =
            new HashSet<string>(StringComparer.Ordinal);

        private bool _disposed;

        public int CapturedPackages { get; private set; }

        public ZineEditorialMailCollector(
            ZineEditorialCycle editorial,
            TimedPostalExchange exchange)
        {
            _editorial = editorial ??
                throw new ArgumentNullException(
                    nameof(editorial));

            _exchange = exchange ??
                throw new ArgumentNullException(
                    nameof(exchange));

            _exchange.MessageOpened += OnMessageOpened;
        }

        private void OnMessageOpened(BacklogItem item)
        {
            if (_disposed || item == null)
                return;

            var package = item.Package;

            // Vain tämän toimituksen vastaanottamat
            // ja avatuksi käsitellyt viestit.
            if (!string.Equals(
                    package.TargetNodeId,
                    _editorial.EditorNodeId,
                    StringComparison.Ordinal))
            {
                return;
            }

            if (!_capturedIds.Add(package.Id))
                return;

            bool collected = false;

            // Tuotetta koskeva eksplisiittinen
            // myönteinen tai kielteinen kanta.
            if (package.Thumb.HasValue &&
                package.FeedbackProductId != null)
            {
                _editorial.RecordProductOpinion(
                    package.SourceNodeId,
                    package.FeedbackProductId,
                    package.Thumb.Value == +1);

                collected = true;
            }

            // Lähettäjän itse ilmoittama osoite.
            // Profiili on vapaaehtoinen.
            if (package.AddressNoticeAddressId != null)
            {
                if (package.DeclaredPublicProfile.HasValue)
                {
                    _editorial.RecordSelfDeclaredContact(
                        package.SourceNodeId,
                        package.AddressNoticeAddressId,
                        package.DeclaredPublicProfile.Value);
                }
                else
                {
                    _editorial.RecordAddress(
                        package.SourceNodeId,
                        package.AddressNoticeAddressId);
                }

                collected = true;
            }

            // Lähettäjän oma, toista solmua
            // koskeva skeptisyysilmoitus.
            //
            // Tämä on julkaistava väite,
            // ei muutos toimittajan skeptisyyteen.
            if (package.SkepticismTargetNodeId != null)
            {
                _editorial.RecordSkepticism(
                    package.SourceNodeId,
                    package.SkepticismTargetNodeId);

                collected = true;
            }

            if (collected)
                CapturedPackages++;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _exchange.MessageOpened -= OnMessageOpened;
            _disposed = true;
        }
    }
}
