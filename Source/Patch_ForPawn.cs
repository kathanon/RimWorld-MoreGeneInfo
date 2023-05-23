using FloatSubMenus;
using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace MoreGeneInfo;
[HarmonyPatch(typeof(Dialog_CreateXenogerm))]
public static class Patch_ForPawn {
    public const float Margin       =   4f;
    public const float GeneMargin   =  14f;
    public const float SearchWidth  = 300f;
    public const float BiostatWidth = 120f;

    public static readonly Vector2 geneSize = GeneCreationDialogBase.GeneSize;
    public static readonly Color outlineColor = new(1f, 1f, 1f, 0.1f);

    private static Pawn pawn = null;
    private static int pawnMetabolism = 0;
    private static readonly HashSet<GeneDef> overriddenGenes = new();


    // Add controls for selecting pawn.

    [HarmonyPrefix]
    [HarmonyPatch("DoBottomButtons")]
    public static void DoBottomButtons(Rect rect, Vector2 ___ButSize, Dialog_CreateXenogerm __instance) {
        float labelWidth = Text.CalcSize(Strings.ForPawn).x;
        float x = rect.x + ___ButSize.x + 4 * Margin;
        var r = new Rect(x, rect.y, labelWidth, ___ButSize.y);

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(r, Strings.ForPawn);
        GenUI.ResetLabelAlign();
        r.x += r.width + Margin;

        r.width = ___ButSize.x;
        string button = pawn?.LabelShortCap ?? Strings.None;
        if (Widgets.ButtonText(r, button)) {
            var menu = new List<FloatMenuOption>{
                new(Strings.None, () => pawn = null),
            };
            var colonists = PawnsFinder.AllMaps_FreeColonists.ToMenu(__instance);
            var prisoners = PawnsFinder.AllMaps_PrisonersOfColony.ToMenu(__instance);
            AddCategories(menu, ("Colonists", colonists), ("Prisoners", prisoners));
            menu.OpenMenu();
        }

        if (pawn != null) {
            r.x += r.width + Margin;
            r.width = r.height;
            Widgets.ThingIcon(r.ExpandedBy(6f), pawn);
        }
    }

    private static void AddCategories(
        List<FloatMenuOption> menu, params (string title, List<FloatMenuOption> list)[] cats) {
        if (cats.Count(x => x.list.Any()) <= 1) {
            foreach ((_, var list) in cats) {
                menu.AddRange(list);
            }
        } else if (cats.Sum(x => x.list.Count) > 14) {
            foreach (var (title, list) in cats) {
                if (list.Any()) {
                    menu.Add(new FloatSubMenu(title, list));
                }
            }
        } else {
            foreach (var (title, list) in cats) {
                menu.Add(new FloatMenuDivider(title));
                menu.AddRange(list);
            }
        }
    }

    private static List<FloatMenuOption> ToMenu(this IEnumerable<Pawn> pawns, Dialog_CreateXenogerm dialog)
        => pawns
            .OrderBy(p => p.LabelShortCap)
            .Select(p => p.MenuOption(() => SetPawn(p, dialog)))
            .ToList();


    private static void SetPawn(Pawn p, Dialog_CreateXenogerm dialog) {
        var handle = Traverse.Create(dialog);
        var xenoNameField = handle.Field<string>("xenotypeName");
        var xenoLockedField = handle.Field<bool>("xenotypeNameLocked");
        string xenoName = xenoNameField.Value;
        bool xenoLocked = xenoLockedField.Value;
        if (!xenoLocked || xenoName == pawn.LabelShortCap) {
            xenoLockedField.Value = true;
            xenoNameField.Value = p.LabelShortCap;
        }

        pawn = p;
        UpdateData(dialog);
    }


    // Show endogenes for selected pawn.

    private static float endoGenesHeight;

    [HarmonyTranspiler]
    [HarmonyPatch("DrawGenes")]
    public static IEnumerable<CodeInstruction> DrawGenes_Transpiler(IEnumerable<CodeInstruction> original) {
        var height = AccessTools.PropertyGetter(typeof(Rect), "height");
        var parameters = new Type[] { typeof(Rect), typeof(float).MakeByRefType(), typeof(Rect), typeof(Dialog_CreateXenogerm) };

        foreach (var instr in original) {
            yield return instr;
            if (instr.Calls(height)) {
                // Argument 1: rect
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                // Argument 2: ref curY
                yield return new CodeInstruction(OpCodes.Ldloca_S, 1);
                // Argument 3: containingRect
                yield return new CodeInstruction(OpCodes.Ldloc_2);
                // Argument 4: this
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                // Call addition method.
                yield return CodeInstruction.Call(typeof(Patch_ForPawn), "DrawEndogenes", parameters);
            }
        }
    }

    private static void DrawEndogenes(Rect rect, ref float curY, Rect visible, Dialog_CreateXenogerm dialog) {
        if (pawn == null) return;

        var traverse = Traverse.Create(dialog);
        bool search  = traverse.Field<QuickSearchWidget>("quickSearchWidget").Value.filter.Active;
        var matching = traverse.Field<HashSet<GeneDef>>("matchingGenes").Value;

        var label = new Rect(10f, curY, rect.width - 16f - 10f, Text.LineHeight);
        Widgets.Label(label, pawn.LabelShortCap + "'s germline genes");
        curY += Text.LineHeight + 3f;

        float startY = curY;
        Rect area = new Rect(0f, curY, rect.width, endoGenesHeight);
        Widgets.DrawRectFast(area, Widgets.MenuSectionBGFillColor);

        area = area.ContractedBy(Margin);
        curY = area.y;
        Rect geneRect = new(area.position, geneSize);
        geneRect.width  += 2 * Margin;
        geneRect.height += 2 * Margin;
        foreach (var geneInst in pawn.genes?.Endogenes ?? new()) {
            if (geneRect.xMax > area.xMax) {
                geneRect.x = area.x;
                geneRect.StepY(GeneMargin);
            }

            var gene = geneInst.def;
            bool overridden = overriddenGenes.Contains(gene);
            if (search && !matching.Contains(gene)) continue;

            // TODO: apply this to other places where overridden genes are displayed as well
            if (!overridden) Widgets.DrawHighlight(geneRect);
            GUI.color = outlineColor;
            Widgets.DrawBox(geneRect); 
            GUI.color = Color.white;

            string extraTooltip = null; // TODO
            GeneUIUtility.DrawGeneDef_NewTemp(gene,
                                              geneRect.ContractedBy(Margin),
                                              GeneType.Endogene,
                                              () => extraTooltip,
                                              doBackground: false,
                                              clickable:    false,
                                              overridden);

            if (Mouse.IsOver(geneRect)) {
                if (overridden) Widgets.DrawHighlight(geneRect);
                Widgets.DrawHighlight(geneRect);
            }
            if (Widgets.ButtonInvisible(geneRect)) {
                Find.WindowStack.Add(new Dialog_InfoCard(gene));
            }

            geneRect.StepX(GeneMargin);
        }

        curY = geneRect.yMax + Margin;
        endoGenesHeight = curY - startY;
        curY += 8;
    }


    // Reset on open.

    [HarmonyPostfix]
    [HarmonyPatch(MethodType.Constructor, typeof(Building_GeneAssembler))]
    public static void Reset(Dialog_CreateXenogerm __instance) {
        pawn = null;
        pawnMetabolism = 0;
        overriddenGenes.Clear();

        // TODO: Add confirmation when selected pawn will have < -5 metabolism with xenogerm
        // Allow creating xenogerm with < -5 metabolism.
        Traverse.Create(__instance).Field<bool>("ignoreRestrictions").Value = true;
    }


    // Update local data when genes list is changed.

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GeneCreationDialogBase), "OnGenesChanged")]
    public static void OnGenesChanged(GeneCreationDialogBase __instance) {
        if (__instance is Dialog_CreateXenogerm dialog && pawn != null) {
            UpdateData(dialog);
        }
    }


    // Alter metabolic efficiency and add pawn name as xenotype name candidate.

    private static bool alterBottomPart = false;
    private static float biostatsLabelWidth;
    private static string biostatsMetDesc;
    private static string lastButton;
    private static int xenoNameGenNo;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GeneCreationDialogBase), nameof(GeneCreationDialogBase.DoWindowContents))]
    public static void DoWindowContents_Pre(GeneCreationDialogBase __instance) {
        alterBottomPart = __instance is Dialog_CreateXenogerm && pawn != null;
        xenoNameGenNo = 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GeneCreationDialogBase), nameof(GeneCreationDialogBase.DoWindowContents))]
    public static void DoWindowContents_Post(GeneCreationDialogBase __instance) 
        => alterBottomPart = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BiostatsTable), nameof(BiostatsTable.Draw))]
    public static void DrawBiostats_Pre(ref int met) {
        if (alterBottomPart) {
            met += pawnMetabolism;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BiostatsTable), nameof(BiostatsTable.Draw))]
    public static void DrawBiostats_Post(Rect rect, int met, int arc) {
        if (alterBottomPart && pawnMetabolism != 0) {
            rect.height /= arc == 0 ? 2 : 3;
            rect.y += rect.height;
            float hungerWidth = Text.CalcSize(biostatsMetDesc).x;
            float x = rect.x + biostatsLabelWidth + BiostatWidth + hungerWidth + 32f;
            int xenoMet = met - pawnMetabolism;
            string text = $"(Xenogerm only: {xenoMet})";
            float width = Text.CalcSize(text).x;
            if (x + width > rect.xMax) {
                text = $"({xenoMet})";
                width = Text.CalcSize(text).x;
            }
            if (x + width <= rect.xMax) {
                Rect r = new(x, rect.y, width, rect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(r, text);
                Text.Anchor = TextAnchor.UpperLeft;
                int pawnMetNow = pawn.genes.GenesListForReading
                    .Where(g => !g.Overridden)
                    .Sum(g => g.def.biostatMet);
                string tooltip = $"Metabolism breakdown:\n  Xenogerm only:\t{Met(xenoMet)}\n  Endogenes add:\t{Met(pawnMetabolism)}\n  Resulting total:\t{Met(met)}\n\n  Pawn currently:\t{Met(pawnMetNow)}";
                TooltipHandler.TipRegion(rect, tooltip);
            }
        }

        string Met(int met) => $"{met}{HungerRate(met)}";

        string HungerRate(int met) {
            if (met == 0) {
                return string.Empty;
            }

            string percent = GeneTuning.MetabolismToFoodConsumptionFactorCurve.Evaluate(met).ToStringPercent();
            return ", x" + percent;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GeneUtility), nameof(GeneUtility.GenerateXenotypeNameFromGenes))]
    public static bool GenerateXenotypeNameFromGenes(ref string __result) {
        if (alterBottomPart && ++xenoNameGenNo == 1 && lastButton == "...") {
            __result = pawn.LabelShortCap;
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonText),
        typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(TextAnchor?))]
    public static void GrabLastButton(string label) 
        => lastButton = label;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BiostatsTable), "MaxLabelWidth")]
    public static void MaxLabelWidth(float __result)
        => biostatsLabelWidth = __result;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BiostatsTable), "MetabolismDescAt")]
    public static void MetabolismDescAt(string __result) 
        => biostatsMetDesc = __result;



    // Utils

    private static void UpdateData(Dialog_CreateXenogerm dialog) {
        overriddenGenes.Clear();
        if (pawn != null && pawn.genes != null) {
            var selected = Traverse.Create(dialog).Property<List<GeneDef>>("SelectedGenes").Value;
            var genes = pawn.genes.Endogenes
                .Select(x => x.def);
            overriddenGenes.AddRange(genes.Where(endo => selected.Any(sel => sel.ConflictsWith(endo))));
            pawnMetabolism = genes
                .Where(x => !overriddenGenes.Contains(x))
                .Sum(x => x.biostatMet);
        } else {
            pawnMetabolism = 0;
        }
    }
}
