namespace SEMM91.Networking.EditorTools
{
    internal sealed class Peak2LocalBotLaunchSpec
    {
        public int BotOrdinal { get; }
        public int Seed { get; }
        public string LogPath { get; }
        public string Arguments { get; }

        public Peak2LocalBotLaunchSpec(
            int botOrdinal,
            int seed,
            string logPath,
            string arguments)
        {
            BotOrdinal = botOrdinal;
            Seed = seed;
            LogPath = logPath;
            Arguments = arguments;
        }
    }
}
