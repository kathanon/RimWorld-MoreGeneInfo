using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace MoreGeneInfo {
    public static class Strings {
        public const string ID = "kathanon.MoreGeneInfo";
        public const string Name = "More Gene Information";

        private const string DefPrefix = "kathanon_MoreGeneInfo_";

        public static string DefColumnTagged = DefPrefix + "TaggedGenes";

        public static readonly string ForPawn = "Create for pawn:";

        public static readonly string None = "(" + "NoneLower".Translate() + ")";

        public static readonly string ReverseTagTip = "Click to open xenotype\nRight-click to select listed tags";
    }
}
