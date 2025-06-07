using HarmonyLib;
using Verse;

namespace VCEPlusUltra
{
    public class VCEPlusUltraMod : Mod
    {
        public VCEPlusUltraMod(ModContentPack pack) : base(pack)
        {
            new Harmony("VCEPlusUltraMod").PatchAll();
        }
    }
}
