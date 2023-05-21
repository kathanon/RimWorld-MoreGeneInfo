using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MoreGeneInfo;

[HarmonyPatch]
public static class Patch_GeneDisplay {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Dialog_InfoCard), nameof(Dialog_InfoCard.DoWindowContents))]
    public static void InfoCard_DoWindowContents_Post(Def ___def, Rect inRect) {
        if (___def is GeneDef gene) {
            GeneTags.For(gene).DoSetupUI(inRect);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GeneUIUtility), "DrawGeneBasics")]
    public static void DrawGeneBasics_Post(GeneDef gene, Rect geneRect) {
        GeneTags.For(gene).DrawIcons(geneRect);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Game), "ExposeSmallComponents")]
    public static void SaveLoad() {
        if (Scribe.mode == LoadSaveMode.LoadingVars) {
            GeneTags.Clear();
        }
        if (Scribe.EnterNode(Strings.ID)) {
            try {
                GeneTags.ExposeDataAll();
            } finally {
                Scribe.ExitNode();
            }
        }
    }
}
