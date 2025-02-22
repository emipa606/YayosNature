using System.Collections.Generic;
using System.Linq;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace YayoNature;

public class Core : ModBase
{
    public static readonly bool using_advBiome;
    public static bool val_worldBiome;
    public static bool val_notice;
    public static bool val_changeWater;
    public static bool val_changeMt;
    public static int val_changeCycle;
    public static int val_changeCycleRandom;
    public static int val_changeTick;
    public static bool val_detectMods;
    public static bool val_testMode;


    public static List<BiomeDef> ar_b;
    public static List<float> ar_b_temp;

    public static readonly List<ThingDef> ar_doNotDestroy_thingDefs = [];


    public static Dictionary<BiomeDef, SettingHandle<bool>> dic_biomeSetting =
        new Dictionary<BiomeDef, SettingHandle<bool>>();

    public static List<BiomeDef> ar_b_no = [];


    // -----------------------------------------


    public static int tickGame;

    private static int i;

    private mapData md;

    private SettingHandle<int> val_changeCycle_s;

    private SettingHandle<int> val_changeCycleRandom_s;

    private SettingHandle<bool> val_changeMt_s;

    private SettingHandle<int> val_changeTick_s;

    private SettingHandle<bool> val_changeWater_s;

    private SettingHandle<bool> val_detectMods_s;

    private SettingHandle<bool> val_notice_s;

    private SettingHandle<bool> val_testMode_s;

    private SettingHandle<bool> val_worldBiome_s;

    static Core()
    {
        if (!ModsConfig.ActiveModsInLoadOrder.Any(mod =>
                mod.PackageId.ToLower().Contains("Mlie.AdvancedBiomes".ToLower())))
        {
            return;
        }

        Log.Message("[YayosNature]: Advanced biomes is used");
        using_advBiome = true;
    }

    public override string ModIdentifier => "YayoNature";


    public override void DefsLoaded()
    {
        val_worldBiome_s = Settings.GetHandle("val_worldBiome", "val_worldBiome_t".Translate(),
            "val_worldBiome_d".Translate(), true);
        val_notice_s = Settings.GetHandle("val_notice", "val_notice_t".Translate(), "val_notice_d".Translate(), true);

        val_changeWater_s = Settings.GetHandle("val_changeWater", "val_changeWater_t".Translate(),
            "val_changeWater_d".Translate(), true);
        val_changeMt_s = Settings.GetHandle("val_changeMt", "val_changeMt_t".Translate(), "val_changeMt_d".Translate(),
            false);

        val_changeCycle_s = Settings.GetHandle("val_changeCycle", "val_changeCycle_t".Translate(),
            "val_changeCycle_d".Translate(), 60);
        val_changeCycleRandom_s = Settings.GetHandle("val_changeCycleRandom", "val_changeCycleRandom_t".Translate(),
            "val_changeCycleRandom_d".Translate(), 15);
        val_changeTick_s = Settings.GetHandle("val_changeTick", "val_changeTick_t".Translate(),
            "val_changeTick_d".Translate(), 8);

        val_detectMods_s = Settings.GetHandle("val_detectMods", "val_detectMods_t".Translate(),
            "val_detectMods_d".Translate(), true);
        val_testMode_s = Settings.GetHandle("val_testMode", "val_testMode_t".Translate(), "val_testMode_d".Translate(),
            false);


        dic_biomeSetting = new Dictionary<BiomeDef, SettingHandle<bool>>();
        foreach (var b in DefDatabase<BiomeDef>.AllDefs.Where(def => def.canBuildBase).OrderBy(def => def.label))
        {
            dic_biomeSetting.Add(b,
                Settings.GetHandle($"noBiome_{b.defName}", $"  - {b.LabelCap}", $"{b.description}", true));
        }


        SettingsChanged();

        ar_doNotDestroy_thingDefs.Add(ThingDefOf.Plant_TreeAnima);
        ar_doNotDestroy_thingDefs.Add(ThingDefOf.Plant_GrassAnima);
        ar_doNotDestroy_thingDefs.Add(ThingDefOf.Plant_TreeGauranlen);
        ar_doNotDestroy_thingDefs.Add(ThingDefOf.Plant_PodGauranlen);
    }

    public override void SettingsChanged()
    {
        val_worldBiome = val_worldBiome_s.Value;
        val_notice = val_notice_s.Value;

        val_changeWater = val_changeWater_s.Value;
        val_changeMt = val_changeMt_s.Value;

        val_changeCycle_s.Value = Mathf.Clamp(val_changeCycle_s.Value, 1, 999);
        val_changeCycle = val_changeCycle_s.Value;

        val_changeCycleRandom_s.Value = Mathf.Clamp(val_changeCycleRandom_s.Value, 0, 999);
        val_changeCycleRandom = val_changeCycleRandom_s.Value;

        val_changeTick_s.Value = Mathf.Clamp(val_changeTick_s.Value, 1, 30);
        val_changeTick = val_changeTick_s.Value;


        val_detectMods = val_detectMods_s.Value;

        val_testMode = val_testMode_s.Value;


        ar_b_no = [];
        foreach (var d in dic_biomeSetting.ToList())
        {
            if (d.Value)
            {
                continue;
            }

            ar_b_no.Add(d.Key);
            Log.Message($"[YayosNature]: '{d.Key.label}' will do not appear");
        }
    }

    public override void WorldLoaded()
    {
        base.WorldLoaded();

        var wd = DataUtility.GetData(Current.Game.World);
        var new_biomeDefForCheckChange = (from b2 in DefDatabase<BiomeDef>.AllDefs select b2.defName).ToList();

        if (wd.biomeDefForCheckChange is not { Count: > 0 })
        {
            wd.biomeDefForCheckChange = new_biomeDefForCheckChange;
        }
        else if (val_detectMods && wd.biomeDefForCheckChange.Count != new_biomeDefForCheckChange.Count)
        {
            var bl = false;
            foreach (var s in new_biomeDefForCheckChange)
            {
                if (!wd.biomeDefForCheckChange.Contains(s))
                {
                    bl = true;
                }
            }

            foreach (var s in wd.biomeDefForCheckChange)
            {
                if (!new_biomeDefForCheckChange.Contains(s))
                {
                    bl = true;
                }
            }

            if (bl)
            {
                Log.Message("[YayosNature]: Biome mod list is changed. reset planet for new biome list");
                resetPlanet();
                wd.biomeDefForCheckChange = new_biomeDefForCheckChange;
            }
        }

        ar_b = wd.ar_b;
        ar_b_temp = wd.ar_b_temp;


        // 모든 베이스 랜덤 바이옴
        trySetRandomBiomeForAllBase();

        // 버그 방지를 위한 초기화
        MapGenerator.SetVar("Elevation", (MapGenFloatGrid)null);
        MapGenerator.SetVar("Fertility", (MapGenFloatGrid)null);
        MapGenerator.SetVar("Caves", (MapGenFloatGrid)null);
    }

    public static void trySetRandomBiomeForAllBase(bool forced = false)
    {
        // 모든 베이스 랜덤 바이옴
        var wd = DataUtility.GetData(Current.Game.World);
        if (!val_worldBiome || !forced && wd.worldRandomSetuped)
        {
            return;
        }

        wd.worldRandomSetuped = true;
        foreach (var s in Find.World.worldObjects.Settlements)
        {
            if (s.Faction == Find.FactionManager.OfPlayer)
            {
                continue;
            }

            var b = getRandomBiome();
            Find.WorldGrid[s.Tile].biome = b;
            Find.WorldGrid[s.Tile].temperature = getBiomeTemp(b);
        }

        foreach (var s in Find.World.worldObjects.Sites)
        {
            var b = getRandomBiome();
            Find.WorldGrid[s.Tile].biome = b;
            Find.WorldGrid[s.Tile].temperature = getBiomeTemp(b);
        }
    }


    public static void resetPlanet(bool render = false)
    {
        var worldGenStepTerrain = new WorldGenStep_Terrain();
        _ = Find.WorldGrid.tiles;

        foreach (var t in Find.WorldGrid.tiles)
        {
            i = Find.WorldGrid.tiles.IndexOf(t);
            t.biome = worldGenStepTerrain.BiomeFrom(t, i);
        }

        DataUtility.GetData(Current.Game.World).ar_b = null;
        ar_b = DataUtility.GetData(Current.Game.World).ar_b;
        DataUtility.GetData(Current.Game.World).ar_b_temp = null;
        ar_b_temp = DataUtility.GetData(Current.Game.World).ar_b_temp;

        trySetRandomBiomeForAllBase(true);

        if (render)
        {
            (Find.World.renderer.layers.Find(a => a is WorldLayer_Terrain) as WorldLayer_Terrain)?.RegenerateNow();
        }


        foreach (var m in Find.Maps)
        {
            if (!m.IsPlayerHome)
            {
                continue;
            }

            var md = DataUtility.GetData(m);
            md.ar_b = null;
        }
    }

    public override void Tick(int currentTick)
    {
        tickGame = Find.TickManager.TicksGame;


        // 첫 사이클 건너뛰기
        if (!val_testMode && tickGame < 5)
        {
            return;
        }

        if (val_notice && tickGame == GenDate.TicksPerHour)
        {
            foreach (var m in Find.Maps)
            {
                if (m.IsPlayerHome)
                {
                    DataUtility.GetData(m).noticeNextBiome();
                }
            }
        }

        // 시작
        foreach (var m in Find.Maps)
        {
            if (!m.IsPlayerHome)
            {
                continue;
            }

            md = DataUtility.GetData(m);
            if (md.nextStartChangeTick == tickGame)
            {
                // 변화 시작
                DataUtility.GetData(m).startChange();
            }
            else if (md.nextStartChangeTick < tickGame)
            {
                // 시작 날짜 초기화
                md.check_nextStartChangeTick();
            }

            if (tickGame % val_changeTick == 0)
            {
                // 맵 틱
                DataUtility.dic_map[m].doTick();
            }
        }
    }

    public static float getBiomeTemp(BiomeDef b)
    {
        return ar_b_temp[ar_b.IndexOf(b)];
    }

    public static void setTileTemp(Tile tile)
    {
        tile.temperature = getBiomeTemp(tile.biome);
    }

    public static BiomeDef getRandomBiome()
    {
        var ar = new List<BiomeDef>();
        ar.AddRange(ar_b);
        ar.RemoveAll(a => ar_b_no.Contains(a));
        return ar.RandomElement();
    }
}