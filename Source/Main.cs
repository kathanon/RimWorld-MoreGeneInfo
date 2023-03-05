using HarmonyLib;
using Verse;
using UnityEngine;
using RimWorld;

namespace MoreGeneInfo;

public class Main : Mod {
    public static Main Instance { get; private set; }

    public Main(ModContentPack content) : base(content) {
        Instance = this;
    }
}
