using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YayoNature;

[HarmonyPatch(typeof(WildPlantSpawner), nameof(WildPlantSpawner.CheckSpawnWildPlantAt))]
internal class WildPlantSpawner_CheckSpawnWildPlantAt
{
    private static Map map;
    private static mapData md;

    private static bool Prefix(WildPlantSpawner __instance, ref bool __result, IntVec3 c, float plantDensity,
        float wholeMapNumDesiredPlants, Map ___map)
    {
        map = ___map;
        md = DataUtility.GetData(map);
        if (md.change)
        {
            if (!md.get_dic_c_gen(c))
            {
                return false;
            }
        }
        else
        {
            return true;
        }

        if (plantDensity <= 0f || c.GetPlant(map) != null || c.GetCover(map) != null || c.GetEdifice(map) != null ||
            map.fertilityGrid.FertilityAt(c) <= 0f || !PlantUtility.SnowAllowsPlanting(c, map))
        {
            __result = false;
            return false;
        }

        var cavePlants = __instance.GoodRoofForCavePlant(c);
        if (__instance.SaturatedAt(c, plantDensity, cavePlants, wholeMapNumDesiredPlants))
        {
            __result = false;
            return false;
        }


        __instance.CalculatePlantsWhichCanGrowAt(c, WildPlantSpawner.tmpPossiblePlants, cavePlants, plantDensity);
        if (!WildPlantSpawner.tmpPossiblePlants.Any())
        {
            __result = false;
            return false;
        }

        __instance.CalculateDistancesToNearbyClusters(c);
        WildPlantSpawner.tmpPossiblePlantsWithWeight.Clear();
        foreach (var thingDef in WildPlantSpawner.tmpPossiblePlants)
        {
            var value = __instance.PlantChoiceWeight(thingDef, c, WildPlantSpawner.distanceSqToNearbyClusters,
                wholeMapNumDesiredPlants, plantDensity);
            WildPlantSpawner.tmpPossiblePlantsWithWeight.Add(new KeyValuePair<ThingDef, float>(thingDef, value));
        }

        if (!WildPlantSpawner.tmpPossiblePlantsWithWeight.TryRandomElementByWeight(x => x.Value, out var result))
        {
            __result = false;
            return false;
        }

        var plant = (Plant)ThingMaker.MakeThing(result.Key);
        plant.Growth = Mathf.Clamp01(Rand.RangeSeeded(0.3f, 1f, Core.tickGame));
        if (plant.def.plant.LimitedLifespan)
        {
            plant.Age = Rand.Range(0, Mathf.Max(plant.def.plant.LifespanTicks - 50, 0));
        }

        GenSpawn.Spawn(plant, c, map);

        __result = true;
        return false;
    }
}