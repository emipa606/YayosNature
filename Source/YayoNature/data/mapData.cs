using System;
using System.Collections.Generic;
using System.Linq;
using ActiveTerrain;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace YayoNature;

public sealed class mapData : IExposable
{
    public readonly Dictionary<IntVec3, ThingDef> dic_c_thing = new Dictionary<IntVec3, ThingDef>();
    private readonly float maxMineableValue = float.MaxValue;
    private List<BiomeDef> _ar_b;
    private MapGenFloatGrid _elevation;
    private MapGenFloatGrid _fertility;
    public BiomeDef _prev_biome;


    private List<float> ar_elevation = [];
    private List<float> ar_fertility = [];
    private List<RoofThreshold> ar_roof = [];
    private IntVec3 c = new IntVec3(0, 0, 0);


    private TerrainDef c_td;
    private TerrainDef c_utd;
    private MapGenFloatGrid caves;

    public bool change;
    public Dictionary<IntVec3, bool> dic_c_gen = new Dictionary<IntVec3, bool>();
    private Building edifice;
    private GenStep_ScatterLumpsMineable genStep_ScatterLumpsMineable;
    private GenStep_Terrain genStep_terrain = new GenStep_Terrain();


    private int i;
    private int j;


    private int k;
    private Map m;
    private bool mapInited;
    public int nextStartChangeTick = 1;
    private Plant plant;
    public float prev_temp;
    public int rpStartIndex = -1;

    // Data
    private List<string> s_ar_b;
    public string s_prev_biome = "";
    public float target_temp;
    private TerrainDef terrainDef;
    public int tick;
    public int tickLength;

    public List<BiomeDef> ar_b
    {
        get
        {
            if (_ar_b != null)
            {
                return _ar_b;
            }

            if (s_ar_b != null)
            {
                _ar_b = (from s in s_ar_b where BiomeDef.Named(s) != null select BiomeDef.Named(s)).ToList();
            }
            else
            {
                _ar_b = makeBiomeList();
                s_ar_b = (from b in _ar_b select b.defName).ToList();
            }

            return _ar_b;
        }
        set
        {
            _ar_b = value;
            s_ar_b = _ar_b == null ? null : (from b in _ar_b select b.defName).ToList();
        }
    }

    public BiomeDef prev_biome
    {
        get
        {
            if (_prev_biome == null)
            {
                _prev_biome = BiomeDef.Named(s_prev_biome);
            }

            return _prev_biome;
        }
        set
        {
            _prev_biome = value;
            s_prev_biome = value.defName;
        }
    }

    private bool c_water => c_td.IsWater || c_utd is { IsWater: true };

    private MapGenFloatGrid elevation
    {
        get
        {
            if (_elevation != null)
            {
                return _elevation;
            }

            _elevation = new MapGenFloatGrid(m);
            if (m == null)
            {
                return _elevation;
            }

            if (ar_elevation.Count >= m.cellIndices.NumGridCells)
            {
                try
                {
                    for (var index = 0; index < m.cellIndices.NumGridCells; index++)
                    {
                        _elevation[m.cellIndices.IndexToCell(index)] = ar_elevation[index];
                    }
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                resetElevationFertility();
            }

            return _elevation;
        }
        set
        {
            _elevation = value;
            var tmp = new float[m.cellIndices.NumGridCells];
            try
            {
                for (var ind = 0; ind < m.cellIndices.NumGridCells; ind++)
                {
                    tmp[ind] = value[m.cellIndices.IndexToCell(ind)];
                }
            }
            catch
            {
                // ignored
            }

            ar_elevation = tmp.ToList();
        }
    }

    private MapGenFloatGrid fertility
    {
        get
        {
            if (_fertility != null)
            {
                return _fertility;
            }

            _fertility = new MapGenFloatGrid(m);
            if (m == null)
            {
                return _fertility;
            }

            if (ar_fertility.Count >= m.cellIndices.NumGridCells)
            {
                try
                {
                    for (var index = 0; index < m.cellIndices.NumGridCells; index++)
                    {
                        _fertility[m.cellIndices.IndexToCell(index)] = ar_fertility[index];
                    }
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                resetElevationFertility();
            }

            return _fertility;
        }
        set
        {
            _fertility = value;
            var tmp = new float[m.cellIndices.NumGridCells];
            try
            {
                for (var ind = 0; ind < m.cellIndices.NumGridCells; ind++)
                {
                    tmp[ind] = value[m.cellIndices.IndexToCell(ind)];
                }
            }
            catch
            {
                // ignored
            }

            ar_fertility = tmp.ToList();
        }
    }


    // Data Save
    public void ExposeData()
    {
        Scribe_Collections.Look(ref s_ar_b, "s_ar_b", LookMode.Value);
        Scribe_Values.Look(ref change, "change");
        Scribe_Values.Look(ref s_prev_biome, "s_prev_biome", "");
        Scribe_Values.Look(ref prev_temp, "prev_temp");
        Scribe_Values.Look(ref target_temp, "target_temp");
        Scribe_Values.Look(ref tick, "tick");
        Scribe_Values.Look(ref rpStartIndex, "rpStartIndex", -1);
        Scribe_Values.Look(ref tickLength, "tickLength");
        Scribe_Values.Look(ref nextStartChangeTick, "nextStartChangeTick", 1);
        Scribe_Collections.Look(ref dic_c_gen, "ar_c_gen", LookMode.Value);
        Scribe_Collections.Look(ref ar_elevation, "ar_elevation", LookMode.Value);
        Scribe_Collections.Look(ref ar_fertility, "ar_fertility", LookMode.Value);
        if (Scribe.mode != LoadSaveMode.LoadingVars)
        {
            return;
        }

        if (ar_elevation == null)
        {
            ar_elevation = [];
        }

        if (ar_fertility == null)
        {
            ar_fertility = [];
        }
    }

    public void setParent(Map _m)
    {
        m = _m;
        if (rpStartIndex < 0)
        {
            rpStartIndex = GenRadial.ar_NumCellsInRadius_rcos[m.Size.x];
        }
    }


    public void debugChangeImmediately(BiomeDef b)
    {
        startChange();
        m.TileInfo.biome = b;
        while (change)
        {
            tick++;
            changeTile();
        }
    }


    public void startChange(BiomeDef _b = null)
    {
        change = true;
        mapInited = false;
        prev_biome = m.TileInfo.biome;
        if (_b == null)
        {
            m.TileInfo.biome = getNextBiome();
        }
        else
        {
            m.TileInfo.biome = _b;
        }

        prev_temp = m.TileInfo.temperature;
        target_temp = Core.getBiomeTemp(m.TileInfo.biome);
        tick = 0;
        tickLength = GenRadial.RadialPattern_r_length - rpStartIndex;
        reset_dic_c_gen();


        //BeachMaker.Cleanup();
        check_nextStartChangeTick();
        resetElevationFertility();
        //prev_biome.baseWeatherCommonalities[0].weather.


        if (Core.val_notice)
        {
            Find.LetterStack.ReceiveLetter(
                "biomeChangeStart_t".Translate(),
                string.Format("biomeChangeStart_d".Translate(),
                    m.TileInfo.feature != null ? m.TileInfo.feature.name : m.Index.ToString(),
                    m.TileInfo.biome.label.Colorize(Color.magenta),
                    $"- {m.TileInfo.biome.label} -\n\n{m.TileInfo.biome.description}"),
                LetterDefOf.NeutralEvent
            );
        }
    }


    public void resetElevationFertility()
    {
        MapGenerator.mapBeingGenerated = m;
        var settlement = Find.WorldObjects.Settlements.Find(a => a.Map == m);
        var mapGenerator = settlement.MapGeneratorDef;
        var ar_genStepWithParams = mapGenerator.genSteps.Select(x => new GenStepWithParams(x, default)).ToList();
        ar_genStepWithParams = ar_genStepWithParams.Concat(settlement.ExtraGenStepDefs).ToList();
        var gs_elevationFertility = ar_genStepWithParams.Find(a => a.def.genStep is GenStep_ElevationFertility);
        gs_elevationFertility.def?.genStep.Generate(m, gs_elevationFertility.parms);

        fertility = MapGenerator.Fertility;
        elevation = MapGenerator.Elevation;
    }


    public void check_nextStartChangeTick()
    {
        if (nextStartChangeTick < Core.tickGame)
        {
            nextStartChangeTick += Mathf.Max(1,
                Core.val_changeCycle + Rand.RangeSeeded(-Core.val_changeCycleRandom, Core.val_changeCycleRandom + 1,
                    Find.World.ConstantRandSeed + Core.tickGame)) * GenDate.TicksPerDay;
        }
    }

    public void stopChange()
    {
        change = false;
        m.TileInfo.temperature = target_temp;
        m.weatherDecider.StartInitialWeather();
        /*
        i = 50000;
        for (j = 0; j < i; j++)
        {
            m.wildPlantSpawner.WildPlantSpawnerTick();
        }
        */
        if (Core.val_notice)
        {
            noticeNextBiome();
        }
    }

    public void noticeNextBiome()
    {
        Find.LetterStack.ReceiveLetter(
            "biomeComingSoon_t".Translate(),
            string.Format("biomeComingSoon_d".Translate(),
                m.TileInfo.feature.name,
                getNextBiome().label.Colorize(Color.magenta),
                $"{Mathf.Max(1, Core.val_changeCycle - Core.val_changeCycleRandom)}~{Core.val_changeCycle + Core.val_changeCycleRandom}",
                $"- {getNextBiome().label} -\n\n{getNextBiome().description}"),
            LetterDefOf.NeutralEvent
        );
    }


    public void doTick()
    {
        //if (core.val_testMode && tick % 100 == 0) Log.Message($"change {change}, local tick {tick}, tick {core.tickGame} / {nextStartChangeTick}");
        if (!change)
        {
            return;
        }

        tick++;
        if (!mapInited)
        {
            mapInited = true;
            BeachMaker.Init(m);
            RockNoises.Init(m);

            var num = 0.7f;
            ar_roof = [];
            var roofThreshold = new RoofThreshold
            {
                roofDef = RoofDefOf.RoofRockThick,
                minGridVal = num * 1.14f
            };
            ar_roof.Add(roofThreshold);
            var roofThreshold2 = new RoofThreshold
            {
                roofDef = RoofDefOf.RoofRockThin,
                minGridVal = num * 1.04f
            };
            ar_roof.Add(roofThreshold2);


            genStep_ScatterLumpsMineable = new GenStep_ScatterLumpsMineable
            {
                maxValue = maxMineableValue
            };
            var num3 = 10f;
            switch (Find.WorldGrid[m.Tile].hilliness)
            {
                case Hilliness.Flat:
                    num3 = 4f;
                    break;
                case Hilliness.SmallHills:
                    num3 = 8f;
                    break;
                case Hilliness.LargeHills:
                    num3 = 11f;
                    break;
                case Hilliness.Mountainous:
                    num3 = 15f;
                    break;
                case Hilliness.Impassable:
                    num3 = 16f;
                    break;
            }

            genStep_ScatterLumpsMineable.countPer10kCellsRange = new FloatRange(num3, num3);
            genStep_ScatterLumpsMineable.minSpacing = 5f;
            genStep_ScatterLumpsMineable.warnOnFail = false;
            var num4 = genStep_ScatterLumpsMineable.CalculateFinalCount(m);
            {
                for (var index = 0; index < (int?)num4; index++)
                {
                    //if (!genStep_ScatterLumpsMineable.TryFindScatterCell(m, out var result))
                    if (!TryFindScatterCell(m, out var result))
                    {
                        break;
                    }

                    ScatterAt(result, m);
                }
            }
        }

        changeTile();
    }

    public TerrainDef getTerrainDef(Map map, IntVec3 intVec3, Building building, MapGenFloatGrid elevationFloatGrid,
        MapGenFloatGrid fertilityFloatGrid, MapGenFloatGrid cavesFloatGrid)
    {
        //td = getCustomTerrainDef(m, c, edifice, elevation, fertility, caves);
        //if (td != null) return td;
        return TerrainFrom(intVec3, map, elevationFloatGrid[intVec3], fertilityFloatGrid[intVec3], null);
        //return TerrainFrom(c, m, elevation[c], fertility[c], null, preferSolid: false);
    }

    private void changeTile()
    {
        m.TileInfo.temperature = ((target_temp - prev_temp) * tick / tickLength) + prev_temp;

        i = rpStartIndex + tick;

        if (i >= GenRadial.RadialPattern_r_length)
        {
            stopChange();
            return;
        }

        c = m.Center + GenRadial.RadialPattern_r[i];
        //if (core.val_testMode && tick % 100 == 0) Log.Message($"change tile {c}");

        if (c.x < 0 || c.x >= m.Size.x || c.z < 0 || c.z >= m.Size.z)
        {
            return;
        }

        set_dic_c_gen(c, true);

        c_td = m.terrainGrid.TerrainAt(c);
        c_utd = m.terrainGrid.UnderTerrainAt(c);

        // 강물 변경금지
        if (c_td.affordances != null && c_td.affordances.Contains(TerrainAffordanceDefOf.MovingFluid) ||
            c_utd is { affordances: not null } &&
            c_utd.affordances.Contains(TerrainAffordanceDefOf.MovingFluid))
        {
            return;
        }

        // 물타일 변경금지
        if (!Core.val_changeWater && c_water)
        {
            return;
        }


        MapGenerator.mapBeingGenerated = m;
        //Settlement settlement = Find.WorldObjects.Settlements.Find(a => a.Map == m);
        //IEnumerable<GenStepWithParams> enumerable = settlement.MapGeneratorDef.genSteps.Select((GenStepDef x) => new GenStepWithParams(x, default(GenStepParams)));
        //genStep_terrain = GenStepDefOf.PreciousLump.genStep as GenStep_Terrain;

        //RiverMaker riverMaker = GenerateRiver(m);
        caves = MapGenerator.Caves;


        edifice = c.GetEdifice(m);
        terrainDef = null;


        if (Core.val_changeMt)
        {
            // 산지붕 변경
            if (c.GetRoof(m) != null && c.GetRoof(m).isNatural && c.GetRoof(m).isThickRoof)
            {
                m.roofGrid.SetRoof(c, RoofDefOf.RoofRockThin);
            }

            if (edifice == null)
            {
                // 산 생성
                if (dic_c_thing.TryGetValue(c, out var value))
                {
                    GenSpawn.Spawn(value, c, m, WipeMode.FullRefund);
                }
                else
                {
                    tryGenerateRock(c);
                }
            }
            else
            {
                // 산 제거
                if (edifice.def.mineable)
                {
                    edifice.Destroy();
                }
            }
        }


        m.snowGrid.SetDepth(c, 0f); // 눈 제거

        // 타일 결정
        terrainDef = getTerrainDef(m, c, edifice, elevation, fertility, caves);
        if (!Core.val_changeWater && terrainDef.IsWater)
        {
            terrainDef = TerrainThreshold.TerrainAtValue(m.Biome.terrainsByFertility, fertility[c]);
            if (Core.val_testMode)
            {
                Log.Message($"target terrain is water -> {terrainDef.label}");
            }
        }

        // 건설된 바닥 유지
        // c 셀이 두겹이라면 덮혀 있다면 SetUnderTerrain
        if (c_utd != null)
        {
            m.terrainGrid.SetUnderTerrain(c, terrainDef);
        }
        else
        {
            SetTerrain(c, terrainDef, m, m.terrainGrid.topGrid, m.terrainGrid.underGrid);
        }


        // 식물 제거
        plant = c.GetPlant(m);
        if (plant != null && (c.Impassable(m) || !Core.ar_doNotDestroy_thingDefs.Contains(plant.def) && !plant.IsCrop))
        {
            plant.Destroy();
        }


        // 식물 생성
        var progress = (float)(i - rpStartIndex) / (GenRadial.RadialPattern_r_length - rpStartIndex);
        if (progress > 0.85f && !Rand.ChanceSeeded(0.001f, Core.tickGame))
        {
            i = Mathf.RoundToInt(150 * progress);
            for (j = 0; j < i; j++)
            {
                m.wildPlantSpawner.WildPlantSpawnerTick();
            }
        }

        MapGenerator.mapBeingGenerated = null;
    }


    private TerrainDef TerrainFrom(IntVec3 intVec3, Map map, float elevationValue, float fertilityValue,
        RiverMaker river)
    {
        TerrainDef terrainAt = null;
        if (river != null)
        {
            terrainAt = river.TerrainAt(intVec3, true);
        }

        var terrainDef2 = BeachMaker.BeachTerrainAt(intVec3, map.Biome);
        if (terrainDef2 == TerrainDefOf.WaterOceanDeep)
        {
            return terrainDef2;
        }

        if (terrainAt is { IsRiver: true })
        {
            return terrainAt;
        }

        if (terrainDef2 != null)
        {
            return terrainDef2;
        }

        if (terrainAt != null)
        {
            return terrainAt;
        }

        foreach (var patchMaker in map.Biome.terrainPatchMakers)
        {
            terrainDef2 = patchMaker.TerrainAt(intVec3, map, fertilityValue);
            if (terrainDef2 != null)
            {
                return terrainDef2;
            }
        }

        if (elevationValue is > 0.55f and < 0.61f)
        {
            return TerrainDefOf.Gravel;
        }

        if (Core.val_changeMt && elevationValue >= 0.61f)
        {
            return RockDefAt(intVec3).building.naturalTerrain;
        }

        terrainDef2 = TerrainThreshold.TerrainAtValue(map.Biome.terrainsByFertility, fertilityValue);
        return terrainDef2 ?? TerrainDefOf.Sand;
    }


    public void SetTerrain(IntVec3 intVec3, TerrainDef newTerr, Map map, TerrainDef[] topGrid, TerrainDef[] underGrid)
    {
        if (Core.using_advBiome)
        {
            try
            {
                ((Action)(() =>
                {
                    // do
                    var terrainAt = map.terrainGrid.TerrainAt(intVec3);
                    if (terrainAt is SpecialTerrain)
                    {
                        map.GetComponent<SpecialTerrainList>().Notify_RemovedTerrainAt(intVec3);
                    }
                }))();
            }
            catch (TypeLoadException)
            {
            }
        }


        if (newTerr == null)
        {
            Log.Error($"Tried to set terrain at {intVec3} to null.");
            return;
        }

        if (Current.ProgramState == ProgramState.Playing)
        {
            map.designationManager.DesignationAt(intVec3, DesignationDefOf.SmoothFloor)?.Delete();
        }

        var num = map.cellIndices.CellToIndex(intVec3);

        if (newTerr.layerable)
        {
            if (underGrid[num] == null)
            {
                if (topGrid[num].passability != Traversability.Impassable)
                {
                    underGrid[num] = topGrid[num];
                }
                else
                {
                    underGrid[num] = TerrainDefOf.Sand;
                }
            }
        }
        else
        {
            underGrid[num] = null;
        }

        topGrid[num] = newTerr;
        DoTerrainChangedEffects(intVec3, map);


        if (!Core.using_advBiome)
        {
            return;
        }

        try
        {
            ((Action)(() =>
            {
                // do
                if (newTerr is SpecialTerrain special)
                {
                    map.GetComponent<SpecialTerrainList>().RegisterAt(special, intVec3);
                }
            }))();
        }
        catch (TypeLoadException)
        {
        }
    }


    private void DoTerrainChangedEffects(IntVec3 intVec3, Map map)
    {
        map.mapDrawer.MapMeshDirty(intVec3, MapMeshFlagDefOf.Terrain, true, false);
        var thingList = intVec3.GetThingList(map);
        for (var num = thingList.Count - 1; num >= 0; num--)
        {
            /*
            if (thingList[num].def.category == ThingCategory.Plant && map.fertilityGrid.FertilityAt(c) < thingList[num].def.plant.fertilityMin && !ar_doNotDestroy_thingDefs.Contains(thingList[num].def))
            {
                thingList[num].Destroy();
            } else if
            */
            if (thingList[num].def.category == ThingCategory.Filth &&
                !FilthMaker.TerrainAcceptsFilth(TerrainAt(intVec3, map), thingList[num].def))
            {
                thingList[num].Destroy();
            }
            else if ((thingList[num].def.IsBlueprint || thingList[num].def.IsFrame) && !GenConstruct.CanBuildOnTerrain(
                         thingList[num].def.entityDefToBuild, thingList[num].Position, map, thingList[num].Rotation,
                         null, ((IConstructible)thingList[num]).EntityToBuildStuff()))
            {
                thingList[num].Destroy(DestroyMode.Cancel);
            }
        }

        map.pathing.RecalculatePerceivedPathCostAt(intVec3);
        var drawerInt = map.terrainGrid.drawerInt;
        drawerInt?.SetDirty();

        map.fertilityGrid.Drawer.SetDirty();
        var regionAt_NoRebuild_InvalidAllowed = map.regionGrid.GetRegionAt_NoRebuild_InvalidAllowed(intVec3);
        if (regionAt_NoRebuild_InvalidAllowed is { Room: not null })
        {
            regionAt_NoRebuild_InvalidAllowed.Room.Notify_TerrainChanged();
        }
    }

    public TerrainDef TerrainAt(IntVec3 intVec3, Map map)
    {
        return map.terrainGrid.topGrid[map.cellIndices.CellToIndex(intVec3)];
    }

    private BiomeDef getNextBiome()
    {
        i = ar_b.IndexOf(m.TileInfo.biome);
        for (k = 0; k < ar_b.Count; k++)
        {
            i++;
            if (i >= ar_b.Count)
            {
                i -= ar_b.Count;
            }

            if (!Core.ar_b_no.Contains(ar_b[i]))
            {
                break;
            }
        }

        return ar_b[i];
    }

    private List<BiomeDef> makeBiomeList()
    {
        var b = m.Biome;
        var ar_tmp = new List<BiomeDef>();
        ar_tmp.AddRange(Core.ar_b);
        var ar_tmp2 = new List<BiomeDef>();

        ar_tmp.Remove(b);

        while (ar_tmp.Count > 0)
        {
            i = Rand.RangeSeeded(0, ar_tmp.Count, Find.World.ConstantRandSeed + Core.tickGame);
            ar_tmp2.Add(ar_tmp[i]);
            ar_tmp.RemoveAt(i);
        }

        ar_tmp2.Add(b);


        // test
        //ar_tmp2[0] = BiomeDefOf.Desert;
        //ar_tmp2[1] = BiomeDefOf.SeaIce;
        if (!Core.val_testMode)
        {
            return ar_tmp2;
        }

        Log.Message("-----------map biome-------------");
        foreach (var b2 in ar_tmp2)
        {
            Log.Message($"({Core.ar_b.IndexOf(b2)}){b2.label} def : {b2.defName}");
        }

        Log.Message("------------------------");

        return ar_tmp2;
    }


    public bool get_dic_c_gen(IntVec3 intVec3)
    {
        try
        {
            return dic_c_gen[intVec3];
        }
        catch
        {
            remake_dic_c_gen();
            return dic_c_gen[intVec3];
        }
    }

    public void set_dic_c_gen(IntVec3 intVec3, bool b)
    {
        try
        {
            dic_c_gen[intVec3] = b;
        }
        catch
        {
            remake_dic_c_gen();
            dic_c_gen[intVec3] = b;
        }
    }

    private void remake_dic_c_gen()
    {
        dic_c_gen = new Dictionary<IntVec3, bool>();
        for (var newX = 0; newX < m.Size.x; newX++)
        {
            for (var newZ = 0; newZ < m.Size.z; newZ++)
            {
                dic_c_gen.Add(new IntVec3(newX, 0, newZ), false);
            }
        }
    }

    private void reset_dic_c_gen()
    {
        try
        {
            foreach (var intVec3 in dic_c_gen.Keys.ToList())
            {
                dic_c_gen[intVec3] = false;
            }
        }
        catch
        {
            remake_dic_c_gen();
        }
    }

    public void tryGenerateRock(IntVec3 intVec3)
    {
        if (m.TileInfo.WaterCovered)
        {
            return;
        }

        var num = 0.7f;


        var num2 = elevation[intVec3];
        if (!(num2 > num))
        {
            return;
        }

        if (caves[intVec3] <= 0f)
        {
            GenSpawn.Spawn(dic_c_thing.TryGetValue(intVec3, out var value) ? value : RockDefAt(intVec3), intVec3, m,
                WipeMode.FullRefund);
        }


        // ReSharper disable once ForCanBeConvertedToForeach
        for (var index = 0; index < ar_roof.Count; index++)
        {
            if (!(num2 > ar_roof[index].minGridVal))
            {
                continue;
            }

            m.roofGrid.SetRoof(intVec3, ar_roof[index].roofDef);

            break;
        }
    }

    private bool IsNaturalRoofAt(IntVec3 intVec3, Map map)
    {
        return intVec3.Roofed(map) && intVec3.GetRoof(map).isNatural;
    }

    public static ThingDef RockDefAt(IntVec3 c)
    {
        ThingDef thingDef = null;
        var num = -999999f;
        foreach (var rockNoise in RockNoises.rockNoises)
        {
            var value = rockNoise.noise.GetValue(c);
            if (!(value > num))
            {
                continue;
            }

            thingDef = rockNoise.rockDef;
            num = value;
        }

        if (thingDef != null)
        {
            return thingDef;
        }

        Log.ErrorOnce($"Did not get rock def to generate at {c}", 50812);
        thingDef = ThingDefOf.Sandstone;

        return thingDef;
    }


    private bool TryFindScatterCell(Map map, out IntVec3 result)
    {
        return CellFinderLoose.TryFindRandomNotEdgeCellWith(5, CanScatterAt, map, out result);


        bool CanScatterAt(IntVec3 intVec3)
        {
            return true;
        }
    }


    private void ScatterAt(IntVec3 intVec3, Map map)
    {
        var thingDef = ChooseThingDef();
        if (thingDef == null)
        {
            return;
        }

        foreach (var c2 in GridShapeMaker.IrregularLump(intVec3, map,
                     thingDef.building.mineableScatterLumpSizeRange.RandomInRange))
        {
            dic_c_thing.TryAdd(c2, thingDef);
        }
    }


    private ThingDef ChooseThingDef()
    {
        if (genStep_ScatterLumpsMineable.forcedDefToScatter != null)
        {
            return genStep_ScatterLumpsMineable.forcedDefToScatter;
        }

        return DefDatabase<ThingDef>.AllDefs.RandomElementByWeightWithFallback(delegate(ThingDef d)
        {
            if (d.building == null)
            {
                return 0f;
            }

            return d.building.mineableThing is { BaseMarketValue: > float.MaxValue }
                ? 0f
                : d.building.mineableScatterCommonality;
        });
    }

    private class RoofThreshold
    {
        public float minGridVal;
        public RoofDef roofDef;
    }
}