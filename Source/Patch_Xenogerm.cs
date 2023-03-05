using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoreGeneInfo;

[HarmonyPatch]
public static class Patch_Xenogerm {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GeneSetHolderBase), nameof(GeneSetHolderBase.GetInspectString))]
    public static void GetInspectString(GeneSetHolderBase __instance, ref string __result) {
        var set = __instance.GeneSet;
        int met = set.MetabolismTotal;
        if (met != 0) {
            var percent = GeneTuning.MetabolismToFoodConsumptionFactorCurve.Evaluate(met).ToStringPercent();
            __result = $"Metabolic efficiency: {met:+#;-#;0} ({percent} hunger)\n{__result}";
        } else {
            __result = "Metabolic efficiency: 0\n" + __result;
        }
    }
}
