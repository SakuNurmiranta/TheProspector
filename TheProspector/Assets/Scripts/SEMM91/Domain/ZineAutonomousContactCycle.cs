
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Solmun päivittäinen uusien kontaktien
    /// tunnustelu zineistä opittujen osoitteiden
    /// ja julkisten profiilien perusteella.
    ///
    /// Käyttää samaa SimulationClockia kuin
    /// muu simulaatio. Ei luo erillistä maailmaa,
    /// postijärjestelmää tai kasettivarastoa.
    /// </summary>
    public sealed class ZineAutonomousContactCycle : IDisposable
    {
        private readonly string _senderNodeId;
        private readonly SimulationClock _clock;
        private readonly ZineContactOutreachPolicy _outreach;
        private readonly ZineFirstContactPostalService _postal;

        private readonly Func<bool> _publicProfile;
        private readonly Func<CassetteCopy> _chooseCopy;

        private readonly HashSet<string> _attemptedTargets =
            new HashSet<string>(StringComparer.Ordinal);

        private int _nextAttemptNumber;
        private bool _disposed;

        public int SentCount { get; private set; }
        public string LastTargetNodeId { get; private set; }
        public PostalShipment LastShipment { get; private set; }

        public ZineAutonomousContactCycle(
            string senderNodeId,
            SimulationClock clock,
            ZineContactOutreachPolicy outreach,
            ZineFirstContactPostalService postal,
            Func<bool> publicProfile,
            Func<CassetteCopy> chooseCopy)
        {
            if (string.IsNullOrWhiteSpace(senderNodeId))
                throw new ArgumentException(
                    "Sender ID cannot be empty.",
                    nameof(senderNodeId));

            _senderNodeId = senderNodeId;
            _clock = clock ??
                throw new ArgumentNullException(nameof(clock));
            _outreach = outreach ??
                throw new ArgumentNullException(nameof(outreach));
            _postal = postal ??
                throw new ArgumentNullException(nameof(postal));
            _publicProfile = publicProfile ??
                throw new ArgumentNullException(
                    nameof(publicProfile));
            _chooseCopy = chooseCopy ??
                throw new ArgumentNullException(
                    nameof(chooseCopy));

            ScheduleNextDay();
        }

        private void ScheduleNextDay()
        {
            int nextDay = checked(_clock.CurrentDay + 1);

            _clock.ScheduleAfter(
                "ZINE-AUTO-CONTACT:" +
                _senderNodeId + ":" + nextDay,
                1,
                OnDay);
        }

        private void OnDay()
        {
            if (_disposed)
                return;

            if (!_outreach.HasPendingOutreach(_senderNodeId))
            {
                TryOneContact();
            }

            if (!_disposed)
                ScheduleNextDay();
        }

        private void TryOneContact()
        {
            bool profile = _publicProfile();

            var candidates = _outreach.GetCandidates(
                _senderNodeId, profile);

            foreach (var candidate in candidates)
            {
                string target = candidate.TargetNodeId;

                // Ei uudelleenyritystä samaan
                // kohteeseen tällä kierroksella.
                if (_attemptedTargets.Contains(target))
                    continue;

                // Valinta tapahtuu käytettävissä
                // olevasta fyysisestä varannosta.
                // Tässä EI tuoteta automaattisesti
                // uusia kasettikopioita.
                CassetteCopy copy = _chooseCopy();

                if (copy == null)
                    return;

                if (copy.HolderNodeId != _senderNodeId)
                    throw new InvalidOperationException(
                        "Selected cassette is not held by sender.");

                string packageId =
                    "ZINE-AUTO-INTRO:" +
                    _senderNodeId + ":" +
                    (++_nextAttemptNumber);

                bool sent = _postal.TrySend(
                    _senderNodeId,
                    profile,
                    target,
                    copy,
                    packageId,
                    out var shipment);

                if (!sent)
                    continue;

                _attemptedTargets.Add(target);

                SentCount++;
                LastTargetNodeId = target;
                LastShipment = shipment;

                // Enintään yksi onnistunut
                // uusi tunnustelu päivässä.
                return;
            }
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
