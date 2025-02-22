using HarmonyLib;
using RimWorld.Planet;

namespace YayoNature;

[HarmonyPatch(typeof(World), nameof(World.ExposeData))]
public class World_ExposeData
{
    private static void Postfix(World __instance)
    {
        DataUtility.GetData(__instance).ExposeData();
    }
}