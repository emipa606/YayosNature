using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace YayoNature;

[HarmonyPatch(typeof(WorldObjectsHolder), nameof(WorldObjectsHolder.Add))]
public class WorldObjectsHolder_Add
{
    private static void Postfix(WorldObject o)
    {
        if (!Core.val_worldBiome || Current.ProgramState != ProgramState.Playing)
        {
            return;
        }

        if (o is not Site && o is not Settlement)
        {
            return;
        }

        var b = Core.getRandomBiome();
        Find.WorldGrid[o.Tile].biome = b;
        Find.WorldGrid[o.Tile].temperature = Core.getBiomeTemp(b);
    }
}