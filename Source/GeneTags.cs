using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MoreGeneInfo;
[StaticConstructorOnStartup]
public class GeneTags : IExposable {
    private const int   PerRow    =  4;
    private const float Margin    =  2f;
    private const float TagSize   = 20f;
    private const float TagMargin =  4f;
    private const float TagStep   = TagSize + TagMargin;

    private const float ReverseIconMargin  = -2f;
    private const float ReverseTagMargin   =  8f;
    private const float ReverseTagSize     = 12f;
    private const float ReverseIconSize    = 24f;
    private const float ReverseOuterMargin =  1f;
    private const int   ReverseTagMarginI  = (int) ReverseTagMargin;
    private const int   ReverseOutMarginI  = (int) (2 * ReverseOuterMargin);
    private const int   ReverseIconWidthI  = (int) (ReverseIconSize + ReverseIconMargin);
    private const int   ReverseTagWidthI   = (int) (ReverseTagSize + ReverseTagMargin);
    public  const int   ReverseHeightI     = (int) (ReverseIconSize + ReverseIconMargin);
    
    private static readonly Color dimmedColor = new(1f, 1f, 1f, 0.1f);

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
    private static readonly Dictionary<int, ReverseList> reverse = new();
    private static readonly List<GeneDef> allGenes;
    private static List<GeneTags> tempEntries;
    private static HashSet<int> tempHiddenTags;

    private GeneDef gene;
    private int geneIndex;
    private List<int> active = new();

    static GeneTags() {
        allGenes = DefDatabase<GeneDef>.AllDefs.ToList();
        allGenes.SortGeneDefs();
        foreach (int i in IconIndices) {
            reverse[i] = new(i);
        }
    }

    public GeneTags() {}

    private GeneTags(GeneDef gene) {
        this.gene = gene;
        geneIndex = allGenes.IndexOf(gene);
    }

    public static GeneTags For(GeneDef gene) {
        if (!table.ContainsKey(gene)) {
            table[gene] = new GeneTags(gene);
        }
        return table[gene];
    }

    private static IEnumerable<int> IconIndices { 
        get {
            for (int i = 0; i < icons.Length; i++) {
                yield return i;
            }
        }
    }

    public void DrawIcons(Rect r) {
        float size = r.width / PerRow - 2 * Margin;
        float xInit = r.xMax - size - Margin;
        float step = size + 2 * Margin;
        Rect icon = new(xInit, r.y + Margin, size, size);
        foreach (int i in active) {
            Graphics.DrawTexture(icon, Icon(i));
            icon.x -= step;
            if (icon.x < r.x) {
                icon.x = xInit;
                icon.y += step;
            }
            if (icon.yMax > r.yMax) break;
        }
    }

    public static void DoReverseIcons(Rect r, Pawn pawn) {
        Widgets.BeginGroup(r);

        var genes = pawn.genes;
        string name = pawn.LabelShortCap;
        var tags = reverse
            .OrderBy(x => x.Key)
            .Select(x => x.Value);
        var visible = tags
            .Where(x => x.Show);
        Rect icon = new(
            ReverseOuterMargin,
            (r.height - ReverseIconSize) / 2,
            ReverseIconSize,
            ReverseIconSize);
        Rect tagIcon = new(
            ReverseOuterMargin,
            (r.height - ReverseTagSize) / 2,
            ReverseTagSize,
            ReverseTagSize);

        bool hit = false;
        foreach (var tag in visible) {
            Graphics.DrawTexture(tagIcon, tag.Icon);
            tagIcon.StepX(ReverseIconMargin);
            icon.x = tagIcon.x;
            foreach (var gene in tag.Genes) {
                bool active = genes.HasEndogene(gene);

                if (Mouse.IsOver(icon)) {
                    hit = true;
                    string has = active ? "has" : "does not have";
                    TooltipHandler.TipRegion(icon, $"{name} {has} \"{gene.LabelCap}\"\n\n{Strings.ReverseTagTip}");
                    Widgets.DrawHighlight(icon.ContractedBy(2f, 0f));
                }

                if (!active) GUI.color = dimmedColor;
                Widgets.DrawTextureFitted(icon, gene.Icon, 1f);
                if (!active) GUI.color = Color.white;

                icon.StepX(ReverseIconMargin);
            }
            icon.x += ReverseTagMargin;
            tagIcon.x = icon.x;
        }
        Widgets.EndGroup();

        if (!hit) {
            TooltipHandler.TipRegion(r, Strings.ReverseTagTip);
        }

        int button = Event.current.button;
        if (Widgets.ButtonInvisible(r, false)) {
            switch (button) {
                case 0:
                    Find.Selector.ClearSelection();
                    Find.Selector.Select(pawn, false);
                    InspectPaneUtility.OpenTab(typeof(ITab_Genes));
                    break;

                case 1:
                    var options = tags.Select(x => x.MenuOption).ToList();
                    Find.WindowStack.Add(new FloatMenu(options));
                    break;
            }
        }
    }

    public static int ReverseIconsWidth()
        => Math.Max(
            reverse
                .OrderBy(x => x.Key)
                .Where(x => x.Value.Show)
                .Select(x => x.Value.n)
                .Sum(n => ReverseIconWidthI * n + ReverseTagWidthI) 
            - ReverseTagMarginI + ReverseOutMarginI,
            0);

    private static Texture2D Icon(int i) 
        => icons[i];

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
            Graphics.DrawTexture(icon, Icon(i));
            if (Widgets.ButtonInvisible(icon)) {
                reverse[i][geneIndex] = tempActive[i] = !tempActive[i];
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
            tempHiddenTags = reverse.Where(x => !x.Value.include).Select(x => x.Key).ToHashSet();
        }

        Scribe_Collections.Look(ref tempEntries, "tags", LookMode.Deep);
        Scribe_Collections.Look(ref tempHiddenTags, "hiddenTags", LookMode.Value);

        if (Scribe.mode == LoadSaveMode.LoadingVars) {
            tempEntries = null;
            if (tempHiddenTags != null) {
                foreach (var rev in reverse) {
                    rev.Value.include = !tempHiddenTags.Contains(rev.Key);
                }
            }
            tempHiddenTags = null;
        }
    }

    public void ExposeData() {
        Scribe_Defs.Look(ref gene, "gene");
        Scribe_Collections.Look(ref active, "active", LookMode.Value);

        if (Scribe.mode == LoadSaveMode.PostLoadInit) {
            table[gene] = this;

            geneIndex = allGenes.IndexOf(gene);
            foreach (int i in IconIndices) {
                reverse[i][geneIndex] = false;
            }
            foreach (int i in active) {
                reverse[i][geneIndex] = true;
            }
        }
    }

    public static void Clear() 
        => table.Clear();


    private class ReverseList {
        private readonly bool[] active = new bool[allGenes.Count];
        private readonly int icon;
        public bool include = true;
        public int n = 0;

        public ReverseList(int i) {
            icon = i;
        }

        public bool this[int i] {
            get => active[i];
            set {
                if (active[i] != value) {
                    n += value ? 1 : -1;
                }
                active[i] = value;
            }
        }

        public Texture2D Icon
            => Icon(icon);

        public IEnumerable<GeneDef> Genes 
            => allGenes.Where((_, i) => active[i]);

        public FloatMenuOption MenuOption 
            => new Option(this);

        public bool Show 
            => n > 0 && include;

        private void Toggle() 
            => include = !include;

        private class Option : FloatMenuOption {
            private readonly ReverseList item;

            public Option(ReverseList item) 
                    : base(" ", item.Toggle, item.Icon, Color.white) {
                this.item = item;
                extraPartWidth = 20f;
                extraPartOnGUI = DrawCheck;
            }

            private bool DrawCheck(Rect r) {
                Widgets.CheckboxDraw(r.x, r.y + (r.height - 20f) / 2, item.include, item.n == 0, 20f);
                return false;
            }

            public override bool DoGUI(Rect rect, bool colonistOrdering, FloatMenu floatMenu) {
                base.DoGUI(rect, colonistOrdering, floatMenu);
                return false;
            }
        }
    }
}
