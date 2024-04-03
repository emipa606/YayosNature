using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YayoNature;

[HarmonyPatch(typeof(WildPlantSpawner), nameof(WildPlantSpawner.CheckSpawnWildPlantAt))]
internal class Patch_WildPlantSpawner_CheckSpawnWildPlantAt
{
    private static Map map;
    private static mapData md;

    private static readonly List<ThingDef> tmpPossiblePlants = AccessTools
        .StaticFieldRefAccess<List<ThingDef>>(AccessTools.Field(typeof(WildPlantSpawner), "tmpPossiblePlants"))
        .Invoke();

    private static readonly List<KeyValuePair<ThingDef, float>> tmpPossiblePlantsWithWeight =
        AccessTools.StaticFieldRefAccess<List<KeyValuePair<ThingDef, float>>>(
            AccessTools.Field(typeof(WildPlantSpawner), "tmpPossiblePlantsWithWeight")).Invoke();

    private static readonly Dictionary<ThingDef, float> distanceSqToNearbyClusters = AccessTools
        .StaticFieldRefAccess<Dictionary<ThingDef, float>>(AccessTools.Field(typeof(WildPlantSpawner),
            "distanceSqToNearbyClusters")).Invoke();

    private static FloatRange InitialGrowthRandomRange = AccessTools
        .StaticFieldRefAccess<FloatRange>(AccessTools.Field(typeof(WildPlantSpawner), "InitialGrowthRandomRange"))
        .Invoke();


    [HarmonyPrefix]
    private static bool Prefix(WildPlantSpawner __instance, ref bool __result, IntVec3 c, float plantDensity,
        float wholeMapNumDesiredPlants, Map ___map)
    {
        map = ___map;
        md = dataUtility.GetData(map);
        if (md.change)
        {
            if (!md.get_dic_c_gen(c))
            {
                return false;
            }
            //plantDensity = 1f;
        }
        else
        {
            return true;
        }

        //


        if (plantDensity <= 0f || c.GetPlant(map) != null || c.GetCover(map) != null || c.GetEdifice(map) != null ||
            map.fertilityGrid.FertilityAt(c) <= 0f || !PlantUtility.SnowAllowsPlanting(c, map))
        {
            __result = false;
            return false;
        }

        var cavePlants = (bool)AccessTools.Method(typeof(WildPlantSpawner), "GoodRoofForCavePlant")
            .Invoke(__instance, [c]);
        if ((bool)AccessTools.Method(typeof(WildPlantSpawner), "SaturatedAt").Invoke(__instance,
                [c, plantDensity, cavePlants, wholeMapNumDesiredPlants]))
        {
            __result = false;
            return false;
        }

        AccessTools.Method(typeof(WildPlantSpawner), "CalculatePlantsWhichCanGrowAt").Invoke(__instance,
            [c, tmpPossiblePlants, cavePlants, plantDensity]);
        if (!tmpPossiblePlants.Any())
        {
            __result = false;
            return false;
        }

        AccessTools.Method(typeof(WildPlantSpawner), "CalculateDistancesToNearbyClusters")
            .Invoke(__instance, [c]);
        tmpPossiblePlantsWithWeight.Clear();
        foreach (var thingDef in tmpPossiblePlants)
        {
            var value = (float)AccessTools.Method(typeof(WildPlantSpawner), "PlantChoiceWeight").Invoke(__instance,
                [thingDef, c, distanceSqToNearbyClusters, wholeMapNumDesiredPlants, plantDensity]);
            tmpPossiblePlantsWithWeight.Add(new KeyValuePair<ThingDef, float>(thingDef, value));
        }

        if (!tmpPossiblePlantsWithWeight.TryRandomElementByWeight(x => x.Value, out var result))
        {
            __result = false;
            return false;
        }

        var plant = (Plant)ThingMaker.MakeThing(result.Key);
        plant.Growth = Mathf.Clamp01(Rand.RangeSeeded(0.3f, 1f, core.tickGame));
        if (plant.def.plant.LimitedLifespan)
        {
            plant.Age = Rand.Range(0, Mathf.Max(plant.def.plant.LifespanTicks - 50, 0));
        }

        GenSpawn.Spawn(plant, c, map);

        __result = true;
        return false;
    }
}