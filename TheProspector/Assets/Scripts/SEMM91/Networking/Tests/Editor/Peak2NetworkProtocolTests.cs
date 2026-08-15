using System.Text;
using NUnit.Framework;

namespace SEMM91.Networking.Tests.Editor
{
    public sealed class Peak2NetworkProtocolTests
    {
        [Test]
        public void CurrentConnectionPayloadIsCompatible()
        {
            Assert.That(
                Peak2NetworkProtocol
                    .IsCompatibleConnectionPayload(
                        Peak2NetworkProtocol
                            .CreateConnectionPayload()),
                Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        public void MissingConnectionPayloadIsRejected(
            string value)
        {
            byte[] payload = value == null
                ? null
                : Encoding.UTF8.GetBytes(value);

            Assert.That(
                Peak2NetworkProtocol
                    .IsCompatibleConnectionPayload(
                        payload),
                Is.False);
        }

        [Test]
        public void DifferentSchemaPayloadIsRejected()
        {
            byte[] payload =
                Encoding.UTF8.GetBytes(
                    "DEMONTAPES|4200|P2-OLD-SCHEMA"
                );

            Assert.That(
                Peak2NetworkProtocol
                    .IsCompatibleConnectionPayload(
                        payload),
                Is.False);
        }
    }
}
