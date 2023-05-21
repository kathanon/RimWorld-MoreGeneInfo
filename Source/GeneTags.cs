using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.UI.CanvasScaler;

namespace MoreGeneInfo;
[StaticConstructorOnStartup]
public class GeneTags : IExposable {
    private const int   PerRow    =  4;
    private const float Margin    =  2f;
    private const float TagSize   = 20f;
    private const float TagMargin =  4f;
    private const float TagStep   = TagSize + TagMargin;

    private static readonly Texture2D[] icons = {
        Widgets.CheckboxOnTex,
        Widgets.CheckboxOffTex,
        TexButton.Minus, 
        TexButton.Plus, 
        TexButton.ReorderUp, 
        TexButton.ReorderDown,
        TexCommand.OpenLinkedQuestTex,
        TexButton.Search,
        TexCommand.Draft,
        TexCommand.AttackMelee,
        TexCommand.Attack,
        Textures.TagBoom,
        TexCommand.DesirePower,
        TexButton.Ingest,
        Pawn_InventoryTracker.DrugTex,
        Textures.TagHeart,
    };
    private static readonly bool[] tempActive = new bool[icons.Length];

    private static readonly Dictionary<GeneDef, GeneTags> table = new();
    private static List<GeneTags> tempEntries;

    private GeneDef gene;
    private List<int> active = new();

    public GeneTags() {}

    private GeneTags(GeneDef gene) {
        this.gene = gene;
    }

    public static GeneTags For(GeneDef gene) {
        if (!table.ContainsKey(gene)) {
            table[gene] = new GeneTags(gene);
        }
        return table[gene];
    }

    public void DrawIcons(Rect r) {
        float size = r.width / PerRow - 2 * Margin;
        float xInit = r.xMax - size - Margin;
        float step = size + 2 * Margin;
        Rect icon = new(xInit, r.y + Margin, size, size);
        foreach (int i in active) {
            Graphics.DrawTexture(icon, icons[i]);
            icon.x -= step;
            if (icon.x < r.x) {
                icon.x = xInit;
                icon.y += step;
            }
            if (icon.yMax > r.yMax) break;
        }
    }

    public void DoSetupUI(Rect r) {
        int n = icons.Length;
        for (int i = 0; i < n; i++) {
            tempActive[i] = false;
        }
        foreach (int i in active) {
            tempActive[i] = true;
        }

        bool change = false;
        Rect icon = new(r.xMax - 36f - TagStep * n, r.y + 22f, TagSize, TagSize);
        var labelText = "Tags:";
        float labelWidth = Text.CalcSize(labelText).x;
        Rect label = new(icon.x - labelWidth - TagMargin, icon.y, labelWidth, 24f);
        Widgets.Label(label, labelText);

        Rect high = icon.ExpandedBy(2f);
        for (int i = 0; i < n; i++) {
            if (tempActive[i]) {
                Widgets.DrawHighlight(high);
            }
            Graphics.DrawTexture(icon, icons[i]);
            if (Widgets.ButtonInvisible(icon)) {
                tempActive[i] = !tempActive[i];
                change = true;
            }
            icon.x += TagStep;
            high.x += TagStep;
        }

        if (change) {
            active.Clear();
            for (int i = 0; i < n; i++) {
                if (tempActive[i]) {
                    active.Add(i);
                }
            }
        }
    }

    public static void ExposeDataAll() {
        if (Scribe.mode == LoadSaveMode.Saving) {
            tempEntries = table.Values.Where(x => x.active.Count > 0).ToList();
        }

        Scribe_Collections.Look(ref tempEntries, "tags", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit) {
            tempEntries = null;
        }
    }

    public void ExposeData() {
        Scribe_Defs.Look(ref gene, "gene");
        Scribe_Collections.Look(ref active, "active", LookMode.Value);

        if (Scribe.mode == LoadSaveMode.PostLoadInit) {
            table[gene] = this;
        }
    }

    public static void Clear() 
        => table.Clear();
}
