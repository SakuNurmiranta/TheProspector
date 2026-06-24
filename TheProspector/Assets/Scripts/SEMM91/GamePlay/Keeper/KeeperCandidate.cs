namespace SEMM91.GamePlay.Keeper
{
    /// <summary>
    /// One active player's candidacy for Keeper selection.
    ///
    /// SceneOutput is the current vertical-slice proxy for
    /// the influence of the player's primary cluster.
    /// </summary>
    public readonly struct KeeperCandidate
    {
        public ulong ClientId { get; }
        public string OwnerEntityId { get; }
        public float SceneOutput { get; }

        public KeeperCandidate(
            ulong clientId,
            string ownerEntityId,
            float sceneOutput)
        {
            ClientId = clientId;

            OwnerEntityId =
                ownerEntityId ?? string.Empty;

            SceneOutput =
                float.IsNaN(sceneOutput) ||
                float.IsInfinity(sceneOutput)
                    ? 0.0f
                    : sceneOutput;
        }
    }
}