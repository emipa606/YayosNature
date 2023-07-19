using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace YayoNature;

public static class dataUtility
{
    public static readonly Dictionary<Map, mapData> dic_map = new Dictionary<Map, mapData>();
    public static readonly Dictionary<World, worldData> dic_world = new Dictionary<World, worldData>();

    public static mapData GetData(Map key)
    {
        dic_map.TryAdd(key, new mapData());
        dic_map[key].setParent(key);
        return dic_map[key];
    }

    public static void Remove(Map key)
    {
        dic_map.Remove(key);
    }


    public static worldData GetData(World key)
    {
        dic_world.TryAdd(key, new worldData());
        dic_world[key].setParent(key);
        return dic_world[key];
    }

    public static void Remove(World key)
    {
        dic_world.Remove(key);
    }
}