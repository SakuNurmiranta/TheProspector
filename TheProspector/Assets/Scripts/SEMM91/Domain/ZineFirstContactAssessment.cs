using System;

namespace SEMM91.Domain
{
    // A recorded outcome of the optional six-axis first-contact
    // response pathway. Silence is not negative feedback.
    public enum ZineFirstContactOutcome
    {
        PositiveReply,
        NegativeReply,
        Silence
    }

    public sealed class ZineFirstContactAssessment
    {
        public string NodeId { get; }
        public string ProductId { get; }
        public string RequestPackageId { get; }
        public int Day { get; }
        public int MatchingAxes { get; }
        public ZineFirstContactOutcome Outcome { get; }

        internal ZineFirstContactAssessment(
            string nodeId,
            string productId,
            string requestPackageId,
            int day,
            int matchingAxes,
            ZineFirstContactOutcome outcome)
        {
            NodeId = nodeId;
            ProductId = productId;
            RequestPackageId = requestPackageId;
            Day = day;
            MatchingAxes = matchingAxes;
            Outcome = outcome;
        }

        public override string ToString()
        {
            return "Day=" + Day +
                   " Node=" + NodeId +
                   " Product=" + ProductId +
                   " Match=" + MatchingAxes + "/6" +
                   " Outcome=" + Outcome +
                   " Request=" + RequestPackageId;
        }
    }
}