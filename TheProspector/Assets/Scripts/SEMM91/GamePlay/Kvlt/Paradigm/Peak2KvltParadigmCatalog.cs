using System.Collections.Generic;

namespace SEMM91.GamePlay.Kvlt.Paradigm
{
    public static class Peak2KvltParadigmCatalog
    {
        public const string SatanAspectId = "SATAN";
        public const string OdinAspectId = "ODIN";

        private static readonly KvltParadigmOpposition[]
            oppositions =
            {
                new KvltParadigmOpposition(
                    SatanAspectId,
                    OdinAspectId)
            };

        public static IReadOnlyList<KvltParadigmOpposition>
            Oppositions => oppositions;
    }
}
