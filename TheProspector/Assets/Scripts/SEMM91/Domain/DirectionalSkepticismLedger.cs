
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Muuttumaton suhdekohtaisen skeptisyyden
    /// havainto.
    /// </summary>
    public sealed class RelationSkepticismSnapshot
    {
        public string ObserverNodeId { get; }
        public string CounterpartNodeId { get; }

        public int BadReferrals { get; }
        public int UnansweredReplies { get; }

        public int Total =>
            BadReferrals + UnansweredReplies;

        internal RelationSkepticismSnapshot(
            string observerNodeId,
            string counterpartNodeId,
            int badReferrals,
            int unansweredReplies)
        {
            ObserverNodeId = observerNodeId;
            CounterpartNodeId = counterpartNodeId;

            BadReferrals = badReferrals;
            UnansweredReplies = unansweredReplies;
        }
    }

    /// <summary>
    /// Suunnattu, tapahtumapohjainen
    /// skeptisyyden kirjanpito.
    ///
    /// B:n skeptisyys A:ta kohtaan
    /// tallennetaan erilleen A:n skeptisyydestä
    /// B:tä kohtaan.
    ///
    /// Kirjanpito ei vielä itsenäisesti
    /// tulkitse postia tai kuuntelupäätöksiä.
    /// </summary>
    public sealed class DirectionalSkepticismLedger
    {
        private sealed class RelationState
        {
            public int BadReferrals;
            public int UnansweredReplies;
        }

        private readonly Dictionary<
            (string Observer, string Counterpart),
            RelationState> _relations =
                new Dictionary<
                    (string Observer, string Counterpart),
                    RelationState>();

        private readonly HashSet<
            (string Observer, string Counterpart, string Product)>
            _penalizedReferrals =
                new HashSet<
                    (string Observer, string Counterpart, string Product)>();

        // Bool ilmaisee, onko tietyn yhteydenoton
        // vastaamattomuudesta syntynyt skeptisyys
        // edelleen aktiivinen.
        //
        // Kompensoitu tapahtuma säilyy rekisterissä,
        // jotta samaa laiminlyöntiä ei kirjata
        // myöhemmin uudelleen.
        private readonly Dictionary<
            (string Observer, string Counterpart, string Message),
            bool> _unanswered =
                new Dictionary<
                    (string Observer, string Counterpart, string Message),
                    bool>();

        public RelationSkepticismSnapshot Get(
            string observerNodeId,
            string counterpartNodeId)
        {
            ValidatePair(
                observerNodeId,
                counterpartNodeId);

            if (!_relations.TryGetValue(
                    (observerNodeId, counterpartNodeId),
                    out var state))
            {
                return new RelationSkepticismSnapshot(
                    observerNodeId,
                    counterpartNodeId,
                    0,
                    0);
            }

            return new RelationSkepticismSnapshot(
                observerNodeId,
                counterpartNodeId,
                state.BadReferrals,
                state.UnansweredReplies);
        }

        /// <summary>
        /// Arvioi aiemmin kommunikoidun suosituksen
        /// suhteessa vastaanottajan valmistuneeseen
        /// kuuntelupäätökseen.
        ///
        /// Tässä vaiheessa vain myönteisen
        /// suosituksen jälkeinen hyllytys
        /// aiheuttaa skeptisyyttä.
        ///
        /// Palauttaa true vain, kun uusi
        /// skeptisyyspiste kirjataan.
        /// </summary>
        public bool AssessReferral(
            string observerNodeId,
            string recommenderNodeId,
            string productId,
            int communicatedThumb,
            ListeningReaction reaction)
        {
            ValidatePair(
                observerNodeId,
                recommenderNodeId);

            ValidateId(productId, nameof(productId));

            if (communicatedThumb < -1 ||
                communicatedThumb > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(communicatedThumb));
            }

            if (!Enum.IsDefined(
                    typeof(ListeningReaction),
                    reaction))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reaction));
            }

            if (communicatedThumb != +1 ||
                reaction != ListeningReaction.OpposeAndShelve)
            {
                return false;
            }

            var key = (
                observerNodeId,
                recommenderNodeId,
                productId);

            // Sama suositus ei voi kasvattaa
            // skeptisyyttä useaan kertaan
            // saman demon perusteella.
            if (!_penalizedReferrals.Add(key))
                return false;

            GetOrCreate(
                observerNodeId,
                recommenderNodeId).BadReferrals++;

            return true;
        }

        /// <summary>
        /// Kirjaa yhden umpeutuneen odotusikkunan.
        /// Vasteaikojen arviointi tehdään muualla.
        /// </summary>
        public bool RecordUnanswered(
            string observerNodeId,
            string counterpartNodeId,
            string messageId)
        {
            ValidatePair(
                observerNodeId,
                counterpartNodeId);

            ValidateId(messageId, nameof(messageId));

            var key = (
                observerNodeId,
                counterpartNodeId,
                messageId);

            if (_unanswered.ContainsKey(key))
                return false;

            _unanswered.Add(key, true);

            GetOrCreate(
                observerNodeId,
                counterpartNodeId).UnansweredReplies++;

            return true;
        }

        /// <summary>
        /// Poistaa juuri yhden aiemmin
        /// kirjatun vastaamattomuuden
        /// tilapäisen skeptisyysvaikutuksen.
        ///
        /// Ei peruuta huonoista suosituksista
        /// kertynyttä skeptisyyttä.
        /// </summary>
        public bool CompensateUnanswered(
            string observerNodeId,
            string counterpartNodeId,
            string messageId)
        {
            ValidatePair(
                observerNodeId,
                counterpartNodeId);

            ValidateId(messageId, nameof(messageId));

            var key = (
                observerNodeId,
                counterpartNodeId,
                messageId);

            if (!_unanswered.TryGetValue(
                    key, out bool active) ||
                !active)
            {
                return false;
            }

            _unanswered[key] = false;

            var state = GetOrCreate(
                observerNodeId,
                counterpartNodeId);

            state.UnansweredReplies--;

            return true;
        }

        private RelationState GetOrCreate(
            string observerNodeId,
            string counterpartNodeId)
        {
            var key = (
                observerNodeId,
                counterpartNodeId);

            if (!_relations.TryGetValue(
                    key, out var state))
            {
                state = new RelationState();
                _relations.Add(key, state);
            }

            return state;
        }

        private static void ValidatePair(
            string observerNodeId,
            string counterpartNodeId)
        {
            ValidateId(
                observerNodeId,
                nameof(observerNodeId));

            ValidateId(
                counterpartNodeId,
                nameof(counterpartNodeId));

            if (string.Equals(
                    observerNodeId,
                    counterpartNodeId,
                    StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Skepticism requires two distinct nodes.");
            }
        }

        private static void ValidateId(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Identifier cannot be empty.",
                    parameterName);
            }
        }
    }
}
