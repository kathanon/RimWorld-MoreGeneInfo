using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MoreGeneInfo;

using Instructions = IEnumerable<CodeInstruction>;

[HarmonyPatch(typeof(GeneCreationDialogBase))]
public static class Patch_StatsFilter {
    public const float Margin       =  12f;
    public const float WidthSearch  = 300f;
    public const float HeightSearch =  24f;
    public const float Width        = 320f;
    public const float Height       =  16f;
    public const float YPos         = 11f - Height - 3f;



    // Add controls

    private static bool drawFilters = false;
    private static Rect drawFiltersArea;
    private static GeneCreationDialogBase drawFiltersDialog;

    [HarmonyPostfix]
    [HarmonyPatch("DrawSearchRect")]
    public static void ActivateDrawSearchRect(Rect rect, float ___searchWidgetOffsetX, GeneCreationDialogBase __instance) {
        drawFilters = true;
        drawFiltersArea = new(rect.width - WidthSearch - ___searchWidgetOffsetX + Margin, YPos, WidthSearch - 2 * Margin, Height);
        drawFiltersDialog = __instance;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Window), "LateWindowOnGUI")]
    public static void DrawSearchRect(ref Rect inRect) {
        if (!drawFilters) return;

        var dialog = drawFiltersDialog;
        drawFiltersArea.position += inRect.position;
        float widthStat = Mathf.Floor((drawFiltersArea.width - 2 * Margin) / 3);
        Rect rectStat = drawFiltersArea.LeftPartPixels(widthStat);
        int n = dialog is Dialog_CreateXenogerm ? 3 : 2;
        Action update = () => UpdateSearch(dialog);
        for (int i = 0; i < n; i++) {
            BiostatFilter.Filters[i].DoGui(rectStat, update);
            rectStat.StepX(Margin);
        }

        drawFilters = false;
        drawFiltersDialog = null;
    }

    public static void UpdateSearch(this GeneCreationDialogBase dialog) 
        => Traverse.Create(dialog).Method("UpdateSearchResults").GetValue();



    // Reset on open.

    [HarmonyPostfix]
    [HarmonyPatch(MethodType.Constructor)]
    public static void Reset(Dialog_CreateXenogerm __instance) {
        foreach (var filter in BiostatFilter.Filters) {
            filter.Reset();
        }
    }



    // Apply filters.

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Dialog_CreateXenogerm), "UpdateSearchResults")]
    public static Instructions UpdateSearchResults_Xenogerm_Transpiler(Instructions original)
        => UpdateSearchResults_Germ_Transpiler(original);

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Dialog_CreateXenogerm), "DrawSection")]
    public static Instructions DrawSection_Xenogerm_Transpiler(Instructions original)
        => FilterActive_Transpiler(original);

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Dialog_CreateXenogerm), "DrawGenepack")]
    public static Instructions DrawGenepack_Xenogerm_Transpiler(Instructions original)
        => FilterActive_Transpiler(original);


    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Dialog_CreateXenotype), "UpdateSearchResults")]
    public static Instructions UpdateSearchResults_Xenotype_Transpiler(Instructions original)
        => UpdateSearchResults_Type_Transpiler(original);

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(Dialog_CreateXenotype), "DrawSection")]
    public static Instructions DrawSection_Xenotype_Transpiler(Instructions original)
        => FilterActive_Transpiler(original);


    private static Instructions UpdateSearchResults_Germ_Transpiler(Instructions original) {
        var geneSet = AccessTools.PropertyGetter(typeof(GeneSetHolderBase), "GeneSet");
        var label   = AccessTools.Field(typeof(Def), "label");
        var parameters = new Type[] { 
            typeof(Genepack), 
            typeof(QuickSearchFilter), 
            typeof(HashSet<Genepack>),
            typeof(HashSet<GeneDef>), 
        };
        List<Label> labels = new();
        int state = 0;

        foreach (var instr in FilterActive_Transpiler(original)) {
            if (instr.Calls(geneSet)) {
                state = 1;
            } else if (state == 1 && instr.opcode == OpCodes.Blt_S) {
                // Call our function.
                yield return AddLabels(CodeInstruction.Call(typeof(Patch_StatsFilter), "AddMatching", parameters));
                state = 2;
            } else if (state == 2) {
                // This loop done, reset for next.
                state = 0;
            }

            if (state == 0 
                || instr.opcode == OpCodes.Ldarg_0 
                || (instr.opcode == OpCodes.Ldfld && !Equals(instr.operand, label))) { 
                yield return AddLabels(instr);
            } else {
                labels.AddRange(instr.labels);
            }
        }

        CodeInstruction AddLabels(CodeInstruction instr) {
            instr.labels.AddRange(labels);
            labels.Clear();
            return instr;
        }
    }

    private static Instructions UpdateSearchResults_Type_Transpiler(Instructions original) {
        var matches = AccessTools.Method(typeof(QuickSearchFilter), "Matches", new Type[] { typeof(string) });
        var label   = AccessTools.Field(typeof(Def), "label");
        var parameters = new Type[] { typeof(QuickSearchFilter), typeof(GeneDef) };
        int numMatches = 0;

        foreach (var instr in FilterActive_Transpiler(original)) {
            if (instr.Calls(matches) && numMatches < 2) {
                // Use different "matches" method for first and second call.
                string name = (numMatches == 0) ? "FilterMatches" : "FilterMatchesCat";
                // Call our "matches" method.
                yield return Call(name, parameters, instr);
                // Setup argument types for second call.
                parameters[1] = typeof(string);
                // Keep track of the number of calls.
                numMatches++;
            } else if (instr.LoadsField(label) && numMatches < 1) {
                // Skip this instruction, we want the GeneDef instead.
            } else { 
                yield return instr;
            }
        }
    }

    private static Instructions FilterActive_Transpiler(Instructions original) {
        var active = AccessTools.PropertyGetter(typeof(QuickSearchFilter), "Active");
        var parameters = new Type[] { typeof(QuickSearchFilter) };

        foreach (var instr in original) {
            if (instr.Calls(active)) {
                // Call our "active" method instead.
                yield return Call("FilterActive", parameters, instr);
            } else {
                yield return instr;
            }
        }
    }

    private static CodeInstruction Call(string name, Type[] parameters, CodeInstruction orig) {
        var call = CodeInstruction.Call(typeof(Patch_StatsFilter), name, parameters);
        call.labels.AddRange(orig.labels);
        return call;
    }


    public static void AddMatching(
            Genepack pack, QuickSearchFilter filter, HashSet<Genepack> packs, HashSet<GeneDef> genes) {
        var set = pack.GeneSet;
        bool match = BiostatFilter.Filters.All(x => x.Matches(set)) 
                  && BiostatFilter.Filters.Any(x => x.Active);
        foreach (var gene in pack.GeneSet.GenesListForReading) {
            if (filter.Active && filter.Matches(gene.label)) {
                genes.Add(gene);
                match = true;
            }
        }
        if (match) {
            packs.Add(pack);
        }
    }

    public static bool FilterActive(QuickSearchFilter filter) {
        return filter.Active || BiostatFilter.Filters.Any(x => x.Active);
    }

    public static bool FilterMatches(QuickSearchFilter filter, GeneDef gene) {
        bool b = filter.Matches(gene.label);
        bool c = BiostatFilter.Filters.All(x => x.Matches(gene));
        bool d = b && c;
        //Log.Error($"{gene.label}: {b} && {c} = {d}");
        return d;
    }

    public static bool FilterMatchesCat(QuickSearchFilter filter, string label) {
        return filter.Active && filter.Matches(label);
    }
}
