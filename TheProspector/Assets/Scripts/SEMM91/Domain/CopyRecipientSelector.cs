
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    /// <summary>
    /// Valitsee kaikki kontaktit, joille solmu
    /// voi myönteisen kopiointipäätöksen jälkeen
    /// pyrkiä lähettämään kulttuurituotteen.
    ///
    /// Valinta perustuu lähettäjän tietämykseen,
    /// ei vastaanottajien todellisiin kokoelmiin.
    /// </summary>
    public sealed class CopyRecipientSelector
    {
        public IReadOnlyList<string> SelectAll(
            string senderNodeId,
            string productId,
            IEnumerable<string> contactNodeIds,
            KnownCollectionRegistry knownCollections,
            Func<string, bool> hasCommunicatedDislike,
            Func<string, bool> canOffer)
        {
            ValidateId(
                senderNodeId,
                nameof(senderNodeId));

            ValidateId(
                productId,
                nameof(productId));

            if (contactNodeIds == null)
                throw new ArgumentNullException(
                    nameof(contactNodeIds));

            if (knownCollections == null)
                throw new ArgumentNullException(
                    nameof(knownCollections));

            if (hasCommunicatedDislike == null)
                throw new ArgumentNullException(
                    nameof(hasCommunicatedDislike));

            if (canOffer == null)
                throw new ArgumentNullException(
                    nameof(canOffer));

            var selected = new List<string>();

            var visited = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (string recipient in contactNodeIds)
            {
                ValidateId(
                    recipient,
                    nameof(contactNodeIds));

                if (!visited.Add(recipient))
                    continue;

                if (recipient == senderNodeId)
                    continue;

                bool? knownToHold =
                    knownCollections.IsKnownToHold(
                        senderNodeId,
                        recipient,
                        productId);

                // true  = vastaanottanut tuotteen
                // false = tunnettu historia,
                //         jossa tuotetta ei ole
                // null  = historia tuntematon
                //
                // Vain false tekee vastaanottajasta
                // ehdokkaan tämän säännön nojalla.
                if (knownToHold != false)
                    continue;

                // Vastaanottajan viestimä kielteinen
                // kanta estää lähettämisen.
                if (hasCommunicatedDislike(recipient))
                    continue;

                // Muut rajoitukset, esimerkiksi
                // jo käynnissä oleva lähetys.
                if (!canOffer(recipient))
                    continue;

                selected.Add(recipient);
            }

            return selected.AsReadOnly();
        }

        private static void ValidateId(
            string value,
            string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "Identifier cannot be empty.",
                    parameterName);
        }
    }
}
