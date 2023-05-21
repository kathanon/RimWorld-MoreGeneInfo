using FloatSubMenus;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MoreGeneInfo;

[StaticConstructorOnStartup]
public abstract class BiostatFilter {

    public const float Margin = 2f;
    public const int NumComp = 4;

    public static readonly BiostatFilter[] Filters = { new Metabolism(), new Complexity(), new Archite() };

    private enum Comparison { Off, Less, Equals, Greater }

    private static readonly Func<int, int, bool>[] CompFuncs = { 
        MatchNone, MatchLess, MatchEqual, MatchGreater 
    };
    private static readonly Texture2D[] CompIcons = { 
        Textures.None, Textures.Less, Textures.Equal, Textures.Greater
    };
    private static readonly Color fadedColor = new(1f, 1f, 1f, 0.5f);

    protected int value = 0;
    private Comparison comp = Comparison.Off;

    public bool Active => comp != Comparison.Off;

    public bool Matches(GeneDef gene) => CompFuncs[(int) comp](value, Stat(gene));

    public bool Matches(GeneSet set)  => CompFuncs[(int) comp](value, Stat(set));

    public void DoGui(Rect rect, Action onChange) {
        var iconRect = rect.LeftPartPixels(rect.height);
        bool active = Active;

        TooltipHandler.TipRegion(iconRect, Name);
        Button(ref iconRect, Icon, active, onChange, null);

        TooltipHandler.TipRegion(iconRect, comp.ToString());
        Button(ref iconRect, CompIcons[(int) comp], true, onChange, IncComp, CompMenu);

        iconRect = iconRect.ContractedBy(2f);
        Button(ref iconRect, TexButton.Minus, active && value > Min, onChange, () => --value);

        rect.x = iconRect.x;
        rect.width = LabelWidth;
        rect.height += 2f;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, Label);
        GenUI.ResetLabelAlign();

        iconRect.x = rect.xMax + Margin;
        Button(ref iconRect, TexButton.Plus, active && value < Max, onChange, () => ++value);
        GUI.color = Color.white;
    }

    public void Reset() {
        value = 0;
        comp = Comparison.Off;
    }

    protected abstract int Stat(GeneDef gene);

    protected abstract int Stat(GeneSet set);

    protected abstract Texture2D Icon { get; }

    protected abstract string Name { get; }

    protected virtual int Min => 0;

    protected virtual int Max => 9;

    protected virtual string Label => value.ToStringCached();

    protected virtual float LabelWidth => Text.CalcSize("5").x;

    private static void Button(ref Rect rect, Texture2D icon, bool active, Action onChange, Action action, Action<Action> rightClick = null) {
        GUI.color = active ? Color.white : fadedColor;
        if (!active || action == null) {
            Widgets.DrawTextureFitted(rect, icon, 1f);
        } else if (Widgets.ButtonImage(rect, icon)) {
            if (rightClick != null && Event.current.button == 1) {
                rightClick(onChange);
            } else {
                action();
                onChange();
            }
        }
        rect.StepX(Margin);
    }

    private void IncComp() {
        if (!Enum.IsDefined(typeof(Comparison), ++comp)) comp = Comparison.Off;
    }

    private void DecComp() {
        if (!Enum.IsDefined(typeof(Comparison), --comp)) comp = Comparison.Greater;
    }

    private void CompMenu(Action onChange) {
        new List<FloatMenuOption>() {
            Option(Comparison.Off), 
            Option(Comparison.Less), 
            Option(Comparison.Equals), 
            Option(Comparison.Greater), 
        }.OpenMenu();

        FloatMenuOption Option(Comparison val) {
            FloatMenuOption opt = new(
                val.ToString(),
                () => { 
                    comp = val;
                    onChange();
                },
                CompIcons[(int) val],
                Color.white);
            if (val == comp) {
                opt.extraPartWidth = 24f;
                opt.extraPartRightJustified = true;
                opt.extraPartOnGUI = r => {
                    Widgets.CheckboxDraw(r.x, r.center.y - 12f, true, false);
                    return false;
                };
            }
            return opt;
        }
    }

    private static bool MatchNone   (int value, int stat) => true; 

    private static bool MatchLess   (int value, int stat) => stat < value; 

    private static bool MatchEqual  (int value, int stat) => stat == value; 

    private static bool MatchGreater(int value, int stat) => stat > value; 

    private class Metabolism : BiostatFilter {
        protected override int Stat(GeneDef gene) => gene.biostatMet;

        protected override int Stat(GeneSet set) => set.MetabolismTotal;

        protected override Texture2D Icon => GeneUtility.METTex.Texture;

        protected override string Name => "Metabolism";

        protected override int Min => -9;

        protected override string Label => value.ToString("+#;-#;0");

        protected override float LabelWidth => Text.CalcSize("+5").x;
    }

    private class Complexity : BiostatFilter {
        protected override int Stat(GeneDef gene) => gene.biostatCpx;

        protected override int Stat(GeneSet set) => set.ComplexityTotal;

        protected override Texture2D Icon => GeneUtility.GCXTex.Texture;

        protected override string Name => "Complexity";
    }

    private class Archite : BiostatFilter {
        protected override int Stat(GeneDef gene) => gene.biostatArc;

        protected override int Stat(GeneSet set) => set.ArchitesTotal;

        protected override Texture2D Icon => GeneUtility.ARCTex.Texture;

        protected override string Name => "Archites";

        protected override int Max => 5;
    }
}
