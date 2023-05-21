using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoreGeneInfo;

[HarmonyPatch]
public static class Patch_AssemblerQueue {
    private static bool disable = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.ExposeData))]
    public static void ExposeData(Building_GeneAssembler __instance) 
        => AssemblerQueues.ExposeData(__instance);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Dialog_CreateXenogerm), "AcceptButtonLabel", MethodType.Getter)]
    public static void AcceptButtonLabel(Building_GeneAssembler ___geneAssembler, ref string __result) {
        if (___geneAssembler.Working) {
            __result = "Enqueue";
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.Start))]
    public static bool Start(
            Building_GeneAssembler __instance, List<Genepack> packs, string xenotypeName, XenotypeIconDef iconDef) {
        if (disable) return true;

        var queue = AssemblerQueues.For(__instance);
        var bill = new XenogermBill(packs, xenotypeName, iconDef);
        int n = queue.Count;
        if (n == 0 || !queue[n - 1].Merge(bill)) {
            queue.Add(bill);
        }
        return !__instance.Working;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Building_GeneAssembler), "Reset")]
    public static void Reset(Building_GeneAssembler __instance) {
        if (disable) return;

        var queue = AssemblerQueues.For(__instance);
        var bill = queue.FirstOrDefault();
        if (bill?.Finish() ?? false) queue.RemoveAt(0);
        queue.FirstOrDefault()?.Start(__instance);
    }

    public static void StartOrig(this Building_GeneAssembler assembler,
                                 List<Genepack> packs,
                                 int architesRequired,
                                 string xenotypeName,
                                 XenotypeIconDef iconDef) {
        disable = true;
        assembler.Start(packs, architesRequired, xenotypeName, iconDef);
        disable = false;
    }

    /*
    public static void ResetOrig(this Building_GeneAssembler assembler) {
        disable = true;
        assembler.Reset();
        disable = false;
    }
    */
}
