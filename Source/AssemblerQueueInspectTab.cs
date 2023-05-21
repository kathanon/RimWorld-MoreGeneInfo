using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MoreGeneInfo;
public class AssemblerQueueInspectTab : ITab {
    public const float EntryHeight =  30f;
    
    public const float EntryStep   = EntryHeight + 3f;

    public static readonly Vector2 WinSize = new Vector2(432f, 480f);

    private Vector2 scrollPosition;
    private int dragging = -1;

    public AssemblerQueueInspectTab() {
        size = WinSize;
        labelKey = "TabBills";
    }

    protected override void FillTab() {
        var assembler = SelThing as Building_GeneAssembler;
        if (assembler == null) return;

        var queue = AssemblerQueues.For(assembler);
        bool started = assembler.ProgressPercent > 0f;
        Rect rect = new Rect(Vector2.zero, WinSize).ContractedBy(10f);
        float labelStep = Text.LineHeight + 2f;
        float heightLabels = (started ? 2 * labelStep + 4f : labelStep);
        Rect view = new(0f, 0f, rect.width - 16f, EntryStep * queue.Count + heightLabels);
        Rect entry = new(1f, 0f, view.width - 2f, EntryHeight);
        Rect label = view.TopPartPixels(Text.LineHeight);

        Widgets.BeginScrollView(rect, ref scrollPosition, view);

        int i = 0, remove = -1;
        if (started) {
            string progress = assembler.ProgressPercent.ToStringPercent().Colorize(ColorLibrary.Teal);
            Widgets.Label(label, $"In progress: ({progress})");

            entry.y = labelStep;
            queue[i].DoInterface(entry, i, ref remove, ref dragging);
            if (remove == i) {
                // TODO: confirmation dialog about removal, if yes remove and cancel from assembler
                remove = -1;
            }
            label.y = entry.yMax + 4f;
            i++;
        }

        if (queue.Count > i) Widgets.Label(label, "Queued:");
        entry.y = label.y + labelStep;
        for (; i < queue.Count; i++) {
            queue[i].DoInterface(entry, i, ref remove, ref dragging);
            entry.y += EntryStep;
        }

        // TODO: dragging

        if (remove >= 0 && remove < queue.Count) {
            queue.RemoveAt(remove);
        }

        Widgets.EndScrollView();
    }
}
