
using System;
using System.Collections.Generic;
using System.Linq;

namespace SEMM91.Domain
{
    public sealed class SimulationClock
    {
        private sealed class ScheduledEvent
        {
            public string Id { get; }
            public Action Execute { get; }

            public ScheduledEvent(
                string id,
                Action execute)
            {
                Id = id;
                Execute = execute;
            }
        }

        // Päivä -> kyseisen päivän tapahtumajono.
        //
        // SortedDictionary pitää päivät järjestyksessä.
        // Queue säilyttää saman päivän tapahtumien
        // rekisteröintijärjestyksen.
        private readonly SortedDictionary<
            int, Queue<ScheduledEvent>> _events =
                new SortedDictionary<
                    int, Queue<ScheduledEvent>>();

        // Kaikki käytetyt tunnisteet, myös suoritetut.
        private readonly HashSet<string> _usedIds =
            new HashSet<string>(
                StringComparer.Ordinal);

        private bool _advancing;

        public int CurrentDay { get; private set; }

        public int PendingCount { get; private set; }

        public SimulationClock()
        {
            CurrentDay = 0;
            PendingCount = 0;
        }

        public void ScheduleAfter(
            string eventId,
            int delayDays,
            Action action)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                throw new ArgumentException(
                    "Event ID cannot be empty.",
                    nameof(eventId));

            if (delayDays < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(delayDays),
                    "Delay must be at least one day.");

            if (action == null)
                throw new ArgumentNullException(
                    nameof(action));

            // Tarkistetaan mahdollinen kokonaisluvun
            // ylivuoto ennen rekisterin muuttamista.
            int dueDay = checked(
                CurrentDay + delayDays);

            if (!_usedIds.Add(eventId))
                throw new ArgumentException(
                    "Event ID has already been used.",
                    nameof(eventId));

            if (!_events.TryGetValue(
                    dueDay, out var queue))
            {
                queue = new Queue<ScheduledEvent>();
                _events.Add(dueDay, queue);
            }

            queue.Enqueue(
                new ScheduledEvent(eventId, action));

            PendingCount++;
        }

        public void AdvanceDays(int days)
        {
            if (days < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(days));

            int targetDay = checked(
                CurrentDay + days);

            AdvanceTo(targetDay);
        }

        public void AdvanceTo(int targetDay)
        {
            if (_advancing)
                throw new InvalidOperationException(
                    "Time advancement is already running.");

            if (targetDay < CurrentDay)
                throw new ArgumentOutOfRangeException(
                    nameof(targetDay),
                    "Cannot move simulation time backwards.");

            _advancing = true;

            try
            {
                while (_events.Count > 0)
                {
                    // SortedDictionaryn ensimmäinen
                    // avain on varhaisin tapahtumapäivä.
                    var next = _events.First();

                    if (next.Key > targetDay)
                        break;

                    CurrentDay = next.Key;

                    // Poistetaan suoritettava tapahtuma
                    // jonosta ennen sen kutsumista.
                    var scheduledEvent =
                        next.Value.Dequeue();

                    if (next.Value.Count == 0)
                    {
                        _events.Remove(next.Key);
                    }

                    PendingCount--;

                    // Tapahtuma voi ajastaa uusia
                    // tulevien päivien tapahtumia.
                    //
                    // Poikkeus keskeyttää ajon.
                    // Jo aloitettua tapahtumaa
                    // ei suoriteta uudelleen.
                    scheduledEvent.Execute();
                }

                // Jos kaikki tapahtumat onnistuivat,
                // siirrytään pyydettyyn päivään.
                CurrentDay = targetDay;
            }
            finally
            {
                _advancing = false;
            }
        }
    }
}
