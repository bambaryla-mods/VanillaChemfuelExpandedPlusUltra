using HarmonyLib;
using Verse;

namespace VCEPlusUltra
{
    public class VCEPlusUltraPipeLimitsMod : Mod
    {
        public VCEPlusUltraPipeLimitsMod(ModContentPack pack) : base(pack)
        {
            new Harmony("VCEPlusUltra.PipeLimits").PatchAll();
        }
    }
}
