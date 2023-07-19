using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace YayoNature;

[HarmonyPatch(typeof(WorldObjectsHolder))]
[HarmonyPatch("Add")]
public class patch_WorldObjectsHolder_Add
{
    private static void Postfix(WorldObject o)
    {
        if (!core.val_worldBiome || Current.ProgramState != ProgramState.Playing)
        {
            return;
        }

        if (o is not Site && o is not Settlement)
        {
            return;
        }

        var b = core.getRandomBiome();
        Find.WorldGrid[o.Tile].biome = b;
        Find.WorldGrid[o.Tile].temperature = core.getBiomeTemp(b);
    }
}