using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MoreGeneInfo {
    [StaticConstructorOnStartup]
    public static class Textures {
        private const string Prefix = Strings.ID + "/";

        public static readonly Texture2D None    = ContentFinder<Texture2D>.Get(Prefix + "None");
        public static readonly Texture2D Less    = ContentFinder<Texture2D>.Get(Prefix + "Less");
        public static readonly Texture2D Equal   = ContentFinder<Texture2D>.Get(Prefix + "Equals");
        public static readonly Texture2D Greater = ContentFinder<Texture2D>.Get(Prefix + "Greater");

        // Tag icons
        public static readonly Texture2D TagBoom  = ContentFinder<Texture2D>.Get("UI/Commands/Detonate");
        public static readonly Texture2D TagHeart = ContentFinder<Texture2D>.Get("UI/Icons/Animal/Bond");
    }
}
