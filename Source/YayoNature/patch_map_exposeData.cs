using HarmonyLib;
using Verse;

namespace YayoNature;

[HarmonyPatch(typeof(Map), nameof(Map.ExposeData))]
public class patch_map_exposeData
{
    private static void Postfix(Map __instance)
    {
        dataUtility.GetData(__instance).ExposeData();
    }
}