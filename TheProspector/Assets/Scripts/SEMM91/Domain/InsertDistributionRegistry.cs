
using System;
using System.Collections.Generic;

namespace SEMM91.Domain
{
    public sealed class InsertDistributionRegistry
    {
        private readonly Dictionary<string, InsertExposure>
            _exposuresById =
                new Dictionary<string, InsertExposure>();

        private readonly Dictionary<string, List<InsertExposure>>
            _exposuresByRecipient =
                new Dictionary<string, List<InsertExposure>>();

        public InsertExposure Distribute(
            string insertId,
            NetworkConnection connection,
            CulturalProduct product)
        {
            if (string.IsNullOrWhiteSpace(insertId))
                throw new ArgumentException(
                    "Insert ID cannot be empty.",
                    nameof(insertId));

            if (connection == null)
                throw new ArgumentNullException(
                    nameof(connection));

            if (product == null)
                throw new ArgumentNullException(
                    nameof(product));

            if (connection.Layer != CommunicationLayer.Insert)
                throw new ArgumentException(
                    "Distribution requires an Insert connection.",
                    nameof(connection));

            if (_exposuresById.ContainsKey(insertId))
                throw new ArgumentException(
                    $"Insert '{insertId}' already exists.",
                    nameof(insertId));

            var exposure = new InsertExposure(
                insertId,
                connection.Id,
                connection.SourceNodeId,
                connection.TargetNodeId,
                product.Id);

            _exposuresById.Add(insertId, exposure);

            if (!_exposuresByRecipient.TryGetValue(
                    connection.TargetNodeId, out var records))
            {
                records = new List<InsertExposure>();

                _exposuresByRecipient.Add(
                    connection.TargetNodeId, records);
            }

            records.Add(exposure);

            return exposure;
        }

        
        public InsertExposure DistributeById(
            string insertId,
            NetworkConnection connection,
            string productId)
        {
            if (string.IsNullOrWhiteSpace(insertId))
                throw new ArgumentException(
                    "Insert ID is required.",
                    nameof(insertId));

            if (connection == null)
                throw new ArgumentNullException(
                    nameof(connection));

            if (connection.Layer != CommunicationLayer.Insert)
                throw new ArgumentException(
                    "Distribution requires an Insert connection.",
                    nameof(connection));

            if (string.IsNullOrWhiteSpace(productId))
                throw new ArgumentException(
                    "Product ID is required.",
                    nameof(productId));

            if (_exposuresById.ContainsKey(insertId))
                throw new ArgumentException(
                    "Insert ID already used.",
                    nameof(insertId));

            var exposure = new InsertExposure(
                insertId,
                connection.Id,
                connection.SourceNodeId,
                connection.TargetNodeId,
                productId);

            _exposuresById.Add(insertId, exposure);

            if (!_exposuresByRecipient.TryGetValue(
                    connection.TargetNodeId,
                    out var list))
            {
                list = new List<InsertExposure>();

                _exposuresByRecipient.Add(
                    connection.TargetNodeId,
                    list);
            }

            list.Add(exposure);

            return exposure;
        }

        
        public IReadOnlyList<InsertExposure> GetForRecipient(
            string recipientNodeId)
        {
            if (string.IsNullOrWhiteSpace(recipientNodeId))
                throw new ArgumentException(
                    "Recipient ID cannot be empty.",
                    nameof(recipientNodeId));

            if (!_exposuresByRecipient.TryGetValue(
                    recipientNodeId, out var records))
                return Array.Empty<InsertExposure>();

            return new List<InsertExposure>(records)
                .AsReadOnly();
        }
    }
}
