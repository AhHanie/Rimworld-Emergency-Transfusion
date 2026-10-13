using RimWorld;
using Verse;

namespace Emergency_Trasnfusion
{
    [DefOf]
    public static class StabilizeJobDefOf
    {
        public static JobDef ET_StabilizeWithHemogen;

        static StabilizeJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(StabilizeJobDefOf));
        }
    }
}
