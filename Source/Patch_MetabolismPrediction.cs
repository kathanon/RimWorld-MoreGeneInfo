using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoreGeneInfo;

[HarmonyPatch]
public static class Patch_MetabolismPrediction {
    private static HashSet<GeneDefWithType> tmpOverridden = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.NonOverriddenGenes), typeof(List<GeneDefWithType>))]
    public static void NonOverriddenGenes(List<GeneDefWithType> geneDefWithTypes, List<GeneDef> __result) {
        if (!ModsConfig.BiotechActive) return;

        bool endo = false, xeno = false;
        foreach (var gene in geneDefWithTypes) {
            if (gene.RandomChosen) {
                if (gene.isXenogene) {
                    xeno = true;
                } else {
                    endo = true;
                }
            }
        }
        if (!(endo && xeno)) return;

        tmpOverridden.Clear();
        for (int i = 0; i < geneDefWithTypes.Count; i++) {
            var a = geneDefWithTypes[i];
            for (int j = i + 1; j < geneDefWithTypes.Count; j++) {
                var b = geneDefWithTypes[j];
                if (a.ConflictsWith(b)) {
                    if (a.RandomChosen || b.RandomChosen) {
                        if (a.isXenogene != b.isXenogene) {
                            tmpOverridden.Add(a.isXenogene ? b : a);
                        }
                    } else if (a.Overrides(b)) {
                        tmpOverridden.Add(b);
                    } else {
                        tmpOverridden.Add(a);
                    }
                }
            }
        }

        __result.Clear();
        foreach (GeneDefWithType gene in geneDefWithTypes) {
            if (!tmpOverridden.Contains(gene)) {
                __result.Add(gene.geneDef);
            }
        }
        tmpOverridden.Clear();
    }
}
