using HarmonyLib;
using Verse;

namespace VCEPlusUltra
{
    public class VCEPlusUltraWasteManagementMod : Mod
    {
        public VCEPlusUltraWasteManagementMod(ModContentPack pack) : base(pack)
        {
            new Harmony("VCEPlusUltra.WasteManagement").PatchAll();
        }
    }
}
