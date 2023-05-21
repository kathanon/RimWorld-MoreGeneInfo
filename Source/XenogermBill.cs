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
public class XenogermBill : IExposable {
    private List<Genepack> genepacks;
    private string name;
    private XenotypeIconDef icon;
    private int amount;

    private int archites;

    public XenogermBill() {}

    public XenogermBill(List<Genepack> genepacks, string name, XenotypeIconDef icon) {
        this.genepacks = genepacks;
        this.name = name;
        this.icon = icon;
        amount = 1;
        CalcArchites();
    }

    public XenogermBill(Building_GeneAssembler assembler) {
        genepacks = Traverse.Create(assembler).Field<List<Genepack>>("genepacksToRecombine").Value.ToList();
        name = assembler.xenotypeName;
        icon = assembler.iconDef;
        amount = 1;
        CalcArchites();
    }

    public bool Merge(XenogermBill other) {
        bool same = name == other.name 
                 && icon == other.icon
                 && archites == other.archites
                 && genepacks.Count == other.genepacks.Count;
        if (same) {
            var set = genepacks.ToHashSet();
            same = other.genepacks.All(set.Remove) && set.Count == 0;
        }

        if (same) {
            amount += other.amount;
        }
        return same;
    }

    private void CalcArchites() => archites = genepacks.Sum(x => x.GeneSet.ArchitesTotal);

    public void ExposeData() {
        Scribe_Collections.Look(ref genepacks, "genepacks", LookMode.Reference);
        Scribe_Values.Look(ref name, "name");
        Scribe_Defs.Look(ref icon, "icon");
        Scribe_Values.Look(ref amount, "amount");

        if (Scribe.mode == LoadSaveMode.PostLoadInit) {
            CalcArchites();
        }
    }

    public bool Finish() 
        => --amount == 0;

    public void Start(Building_GeneAssembler assembler) 
        => assembler.StartOrig(genepacks.ToList(), archites, name, icon);

    public void DoInterface(Rect rect, int index, ref int remove, ref int drag) {
        // Highlight every other entry.
        if (index % 2 == 0) {
            Widgets.DrawAltRect(rect);
        }

        // From left side:
        // Drag handle
        Rect iconRect = new(rect.x + 2f, rect.y + (rect.height - 24f) / 2, 24f, 24f);
        if (Widgets.ButtonImageDraggable(iconRect, TexButton.DragHash) == Widgets.DraggableResult.Dragged) {
            drag = index;
        }

        // Xenotype icon
        iconRect.StepX(4f);
        GUI.DrawTexture(iconRect, icon.Icon);

        // From right side:
        // Delete button
        float left = iconRect.xMax + 4f;
        iconRect.x = rect.xMax - 2f - iconRect.width;
        if (Widgets.ButtonImage(iconRect, TexButton.DeleteX)) {
            remove = index;
        }

        // Increase amount
        Rect button = new(iconRect.x - 24f - 4f, iconRect.y + 2f, 20f, 20f);
        bool noDown = amount <= 1;
        if (Widgets.ButtonImage(button, TexButton.Plus)) amount++;
        button.x -= button.width;

        // Decrease amount
        if (noDown) {
            GUI.color = new(1f, 1f, 1f, 0.5f);
            GUI.DrawTexture(button, TexButton.Minus);
            GUI.color = Color.white;
        } else if (Widgets.ButtonImage(button, TexButton.Minus)) {
            amount--;
        }

        // Amount
        string amountStr = $"{amount}x";
        float width = Text.CalcSize(amountStr).x;
        float right = button.x - 4f - width;
        Rect label = rect;
        label.xMin = right;
        label.width = width;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(label, amountStr);

        // Xenotype name
        label.xMin = left;
        label.xMax = right - 2f;
        Widgets.Label(label, name);

        GenUI.ResetLabelAlign();
    }
}
