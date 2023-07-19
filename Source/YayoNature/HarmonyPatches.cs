using System.Reflection;
using HarmonyLib;
using Verse;

namespace YayoNature;

public class HarmonyPatches : Mod
{
    public HarmonyPatches(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("com.yayo.nature");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }
}