using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoreGeneInfo;
[HarmonyPatch]
public static class Integration_Numbers {
    private static readonly bool active =
        LoadedModManager.RunningMods.Any(m => m.PackageId == "mehni.numbers");
    private static MethodBase addColumn = null;
    private static readonly PawnColumnDef columnDef = 
        DefDatabase<PawnColumnDef>.GetNamedSilentFail(Strings.DefColumnTagged);
    private static PawnColumnDef[] addColumnParams = { columnDef };

    [HarmonyPrepare]
    public static bool Active()
        => active;

    [HarmonyTargetMethod]
    public static MethodBase Target() 
        => AccessTools.Method("Numbers.OptionsMaker:General");

    [HarmonyPostfix]
    public static IEnumerable<FloatMenuOption> Options_General_Post(IEnumerable<FloatMenuOption> __result, object __instance) {
        foreach (var option in __result) {
            yield return option;
        }
        if (addColumn == null) { 
            addColumn = AccessTools.Method("Numbers.OptionsMaker:AddPawnColumnAtBestPositionAndRefresh");
        }
        yield return new FloatMenuOption(columnDef.label, () => addColumn.Invoke(__instance, addColumnParams));
    }
}
