using Verse;

namespace VCEPlusUltra
{
    public class CompProperties_QualityOffset : CompProperties
    {
        public int qualityOffset;
        public CompProperties_QualityOffset()
        {
            this.compClass = typeof(CompQualityOffset);
        }
    }
    public class CompQualityOffset : ThingComp
    {
        public CompProperties_QualityOffset Props => base.props as CompProperties_QualityOffset;
    }
}
