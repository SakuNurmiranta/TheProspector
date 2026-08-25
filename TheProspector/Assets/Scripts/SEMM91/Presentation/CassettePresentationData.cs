namespace SEMM91.Presentation
{
    public readonly struct CassettePresentationData
    {
        public readonly string DemoTapeId;
        public readonly string DisplayName;

        public CassettePresentationData(
            string demoTapeId,
            string displayName)
        {
            DemoTapeId = demoTapeId;
            DisplayName = displayName;
        }
    }
}