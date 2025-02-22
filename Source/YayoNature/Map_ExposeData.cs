using HarmonyLib;
using Verse;

namespace YayoNature;

[HarmonyPatch(typeof(Map), nameof(Map.ExposeData))]
public class Map_ExposeData
{
    private static void Postfix(Map __instance)
    {
        DataUtility.GetData(__instance).ExposeData();
    }
}