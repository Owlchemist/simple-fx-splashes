
using RimWorld;
using Verse;

namespace SimpleFxSplashes
{
    [StaticConstructorOnStartup]
    public static class ResourceBank
    {
		[DefOf]
		public static class FleckDefOf
        {
            public static FleckDef Owl_Splash;
        }

        [DefOf]
		public static class StuffCategoryDefOf
        {
            public static StuffCategoryDef Metallic;
            public static StuffCategoryDef Stony;
        }
    }
}
