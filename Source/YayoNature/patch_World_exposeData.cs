using HarmonyLib;
using RimWorld.Planet;

namespace YayoNature;

[HarmonyPatch(typeof(World), nameof(World.ExposeData))]
public class patch_World_exposeData
{
    private static void Postfix(World __instance)
    {
        dataUtility.GetData(__instance).ExposeData();
    }
}