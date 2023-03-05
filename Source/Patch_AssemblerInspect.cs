using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.Code;

namespace MoreGeneInfo;
[HarmonyPatch]
public static class Patch_AssemblerInspect {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.GetInspectString))]
    public static void GetInspectString(Building_GeneAssembler __instance, ref string __result) {
        int storageUsed = 0, storageMax = 0;
        var banks = __instance.ConnectedFacilities
            .Select(x => x.TryGetComp<CompGenepackContainer>())
            .Where(x => x != null);
        foreach (var bank in banks) {
            storageUsed += bank.ContainedGenepacks.Count;
            storageMax += bank.Props.maxCapacity;
        }
        int complexity = __instance.MaxComplexity();
        var nl = __result.NullOrEmpty() ? "" : "\n";
        __result = $"Attached gene storage: {storageUsed}/{storageMax}, max complexity: {complexity}{nl}{__result}";
    }
}
