using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MoreGeneInfo;
public class PawnColumnWorker_TaggedGenes : PawnColumnWorker {


    public override void DoCell(Rect rect, Pawn pawn, PawnTable table) 
        => GeneTags.DoReverseIcons(rect, pawn);

    public override int GetMinCellHeight(Pawn pawn) 
        => GeneTags.ReverseHeightI;

    public override int GetMaxWidth(PawnTable table) 
        => GetMinWidth(table);

    public override int GetMinWidth(PawnTable table) 
        => GeneTags.ReverseIconsWidth();
}
