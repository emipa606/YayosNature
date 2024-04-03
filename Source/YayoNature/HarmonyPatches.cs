using System.Reflection;
using HarmonyLib;
using Verse;

namespace YayoNature;

public class HarmonyPatches : Mod
{
    public HarmonyPatches(ModContentPack content) : base(content)
    {
        new Harmony("com.yayo.nature").PatchAll(Assembly.GetExecutingAssembly());
    }
}