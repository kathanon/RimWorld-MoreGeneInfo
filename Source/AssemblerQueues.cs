using RimWorld;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoreGeneInfo;
public static class AssemblerQueues {
    private static readonly ConditionalWeakTable<Building_GeneAssembler, List<XenogermBill>> table = new();

    public static List<XenogermBill> For(Building_GeneAssembler assembler) 
        => table.GetOrCreateValue(assembler);

    public static void ExposeData(Building_GeneAssembler assembler) {
        List<XenogermBill> queue = For(assembler);

        Scribe_Collections.Look(ref queue, Strings.QueueTag, LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.LoadingVars) {
            table.Remove(assembler);
            if (queue != null) {
                table.Add(assembler, queue);
            }
        } else if (Scribe.mode == LoadSaveMode.PostLoadInit && !queue.Any() && assembler.Working) {
            queue.Add(new(assembler));
        }
    }
}
