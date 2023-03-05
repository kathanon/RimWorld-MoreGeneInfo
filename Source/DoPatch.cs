using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoreGeneInfo;

[StaticConstructorOnStartup]
public class DoPatch {
    static DoPatch() {
        var harmony = new Harmony(Strings.ID);
        harmony.PatchAll();
    }
}
