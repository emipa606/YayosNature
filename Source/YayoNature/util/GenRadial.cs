using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace YayoNature;

public static class GenRadial
{
    private const int RadialPatternCount = 160000;
    public static IntVec3[] ManualRadialPattern;

    public static IntVec3[] RadialPattern;
    public static IntVec3[] RadialPattern_r;
    public static int RadialPattern_r_length;
    public static int[] ar_NumCellsInRadius_rcos;

    private static float[] RadialPatternRadii;

    private static int setupTryCount;
    private static bool isSetuped;

    static GenRadial()
    {
        setupAuto();
    }

    public static float MaxRadialPatternRadius => RadialPatternRadii[RadialPatternRadii.Length - 1];

    public static void setupAuto()
    {
        while (!isSetuped && setupTryCount < 5)
        {
            try
            {
                setupTryCount++;
                setup();
            }
            catch
            {
                Log.Message("GenRadial fail and retry");
            }
        }
    }


    public static void setup()
    {
        ManualRadialPattern = new IntVec3[49];
        RadialPattern = new IntVec3[RadialPatternCount];
        RadialPatternRadii = new float[RadialPatternCount];
        SetupManualRadialPattern();
        SetupRadialPattern();
        RadialPattern_r = RadialPattern;
        RadialPattern_r = RadialPattern_r.ToList().Reverse<IntVec3>().ToArray();

        ar_NumCellsInRadius_rcos = new int[360];
        for (var i = 0; i < ar_NumCellsInRadius_rcos.Length; i++)
        {
            ar_NumCellsInRadius_rcos[i] = NumCellsInRadius_rcos(i);
        }

        isSetuped = true;
        RadialPattern_r_length = RadialPattern_r.Length;
    }


    private static void SetupManualRadialPattern()
    {
        ManualRadialPattern[0] = new IntVec3(0, 0, 0);
        ManualRadialPattern[1] = new IntVec3(0, 0, -1);
        ManualRadialPattern[2] = new IntVec3(1, 0, 0);
        ManualRadialPattern[3] = new IntVec3(0, 0, 1);
        ManualRadialPattern[4] = new IntVec3(-1, 0, 0);
        ManualRadialPattern[5] = new IntVec3(1, 0, -1);
        ManualRadialPattern[6] = new IntVec3(1, 0, 1);
        ManualRadialPattern[7] = new IntVec3(-1, 0, 1);
        ManualRadialPattern[8] = new IntVec3(-1, 0, -1);
        ManualRadialPattern[9] = new IntVec3(2, 0, 0);
        ManualRadialPattern[10] = new IntVec3(-2, 0, 0);
        ManualRadialPattern[11] = new IntVec3(0, 0, 2);
        ManualRadialPattern[12] = new IntVec3(0, 0, -2);
        ManualRadialPattern[13] = new IntVec3(2, 0, 1);
        ManualRadialPattern[14] = new IntVec3(2, 0, -1);
        ManualRadialPattern[15] = new IntVec3(-2, 0, 1);
        ManualRadialPattern[16] = new IntVec3(-2, 0, -1);
        ManualRadialPattern[17] = new IntVec3(-1, 0, 2);
        ManualRadialPattern[18] = new IntVec3(1, 0, 2);
        ManualRadialPattern[19] = new IntVec3(-1, 0, -2);
        ManualRadialPattern[20] = new IntVec3(1, 0, -2);
        ManualRadialPattern[21] = new IntVec3(2, 0, 2);
        ManualRadialPattern[22] = new IntVec3(-2, 0, -2);
        ManualRadialPattern[23] = new IntVec3(2, 0, -2);
        ManualRadialPattern[24] = new IntVec3(-2, 0, 2);
        ManualRadialPattern[25] = new IntVec3(3, 0, 0);
        ManualRadialPattern[26] = new IntVec3(0, 0, 3);
        ManualRadialPattern[27] = new IntVec3(-3, 0, 0);
        ManualRadialPattern[28] = new IntVec3(0, 0, -3);
        ManualRadialPattern[29] = new IntVec3(3, 0, 1);
        ManualRadialPattern[30] = new IntVec3(-3, 0, -1);
        ManualRadialPattern[31] = new IntVec3(1, 0, 3);
        ManualRadialPattern[32] = new IntVec3(-1, 0, -3);
        ManualRadialPattern[33] = new IntVec3(-3, 0, 1);
        ManualRadialPattern[34] = new IntVec3(3, 0, -1);
        ManualRadialPattern[35] = new IntVec3(-1, 0, 3);
        ManualRadialPattern[36] = new IntVec3(1, 0, -3);
        ManualRadialPattern[37] = new IntVec3(3, 0, 2);
        ManualRadialPattern[38] = new IntVec3(-3, 0, -2);
        ManualRadialPattern[39] = new IntVec3(2, 0, 3);
        ManualRadialPattern[40] = new IntVec3(-2, 0, -3);
        ManualRadialPattern[41] = new IntVec3(-3, 0, 2);
        ManualRadialPattern[42] = new IntVec3(3, 0, -2);
        ManualRadialPattern[43] = new IntVec3(-2, 0, 3);
        ManualRadialPattern[44] = new IntVec3(2, 0, -3);
        ManualRadialPattern[45] = new IntVec3(3, 0, 3);
        ManualRadialPattern[46] = new IntVec3(3, 0, -3);
        ManualRadialPattern[47] = new IntVec3(-3, 0, 3);
        ManualRadialPattern[48] = new IntVec3(-3, 0, -3);
    }

    private static void SetupRadialPattern()
    {
        var list = new List<IntVec3>();
        var list2 = new List<IntVec3>();
        for (var i = -200; i < 200; i++)
        {
            for (var j = -200; j < 200; j++)
            {
                list.Add(new IntVec3(i, 0, j));
            }
        }

        list2.AddRange(list);
        list.Sort(delegate(IntVec3 A, IntVec3 B)
        {
            float num = A.LengthHorizontalSquared;
            float num2 = B.LengthHorizontalSquared;
            if (num < num2)
            {
                return -1;
            }

            return num != num2 ? 1 : 0;
        });
        var l = 0;
        list2.Sort(delegate(IntVec3 A, IntVec3 B)
        {
            float num = A.LengthHorizontalSquared;
            float num2 = B.LengthHorizontalSquared;
            if (num < num2)
            {
                return -1;
            }

            if (A.DistanceTo(IntVec3.Zero) - B.DistanceTo(IntVec3.Zero) > 10f) // 노이즈 범위
            {
                return num != num2 ? 1 : 0;
            }

            l++;
            return num != num2 ? Rand.ChanceSeeded(0.5f, l) ? 1 : -1 : 0; // 노이즈 확률. 높을수록 노이즈 없음 // 시드값 수정시 생성 실패 할 수 있음
        });
        for (var k = 0; k < RadialPatternCount; k++)
        {
            RadialPattern[k] = list2[k];
            RadialPatternRadii[k] = list[k].LengthHorizontal;
        }
    }

    public static int NumCellsToFillForRadius_ManualRadialPattern(int radius)
    {
        switch (radius)
        {
            case 0:
                return 1;
            case 1:
                return 9;
            case 2:
                return 21;
            case 3:
                return 37;
            default:
                Log.Error("NumSquares radius error");
                return 0;
        }
    }

    public static int NumCellsInRadius(float radius)
    {
        if (radius >= MaxRadialPatternRadius)
        {
            Log.Error($"Not enough squares to get to radius {radius}. Max is {MaxRadialPatternRadius}");
            return RadialPatternCount;
        }

        var num = radius + float.Epsilon;
        for (var i = 0; i < RadialPatternCount; i++)
        {
            if (RadialPatternRadii[i] > num)
            {
                return i;
            }
        }

        return RadialPatternCount;
    }

    public static int NumCellsInRadius_r(float radius)
    {
        if (radius >= MaxRadialPatternRadius)
        {
            Log.Error($"Not enough squares to get to radius {radius}. Max is {MaxRadialPatternRadius}");
            return RadialPatternCount;
        }

        var num = radius + float.Epsilon;
        for (var i = 0; i < RadialPatternCount; i++)
        {
            if (RadialPatternRadii[i] > num)
            {
                return RadialPatternCount - i;
            }
        }

        return RadialPatternCount - RadialPatternCount;
    }

    public static int NumCellsInRadius_rcos(float radius)
    {
        radius = (radius / Mathf.Cos(0.785398f) / 2) + 7f;
        if (radius >= MaxRadialPatternRadius)
        {
            Log.Error($"Not enough squares to get to radius {radius}. Max is {MaxRadialPatternRadius}");
            return RadialPatternCount;
        }

        var num = radius + float.Epsilon;
        for (var i = 0; i < RadialPatternCount; i++)
        {
            if (RadialPatternRadii[i] > num)
            {
                return RadialPatternCount - i;
            }
        }

        return RadialPatternCount - RadialPatternCount;
    }

    public static float RadiusOfNumCells(int numCells)
    {
        return RadialPatternRadii[numCells];
    }

    public static IEnumerable<IntVec3> RadialPatternInRadius(float radius)
    {
        var numSquares = NumCellsInRadius(radius);
        for (var i = 0; i < numSquares; i++)
        {
            yield return RadialPattern[i];
        }
    }

    public static IEnumerable<IntVec3> RadialCellsAround(IntVec3 center, float radius, bool useCenter)
    {
        var numSquares = NumCellsInRadius(radius);
        for (var i = !useCenter ? 1 : 0; i < numSquares; i++)
        {
            yield return RadialPattern[i] + center;
        }
    }

    public static IEnumerable<IntVec3> RadialCellsAround(IntVec3 center, float minRadius, float maxRadius)
    {
        var numSquares = NumCellsInRadius(maxRadius);
        for (var i = 0; i < numSquares; i++)
        {
            if (RadialPattern[i].LengthHorizontal >= minRadius)
            {
                yield return RadialPattern[i] + center;
            }
        }
    }

    public static IEnumerable<Thing> RadialDistinctThingsAround(IntVec3 center, Map map, float radius, bool useCenter)
    {
        var numCells = NumCellsInRadius(radius);
        HashSet<Thing> returnedThings = null;
        for (var i = !useCenter ? 1 : 0; i < numCells; i++)
        {
            var c = RadialPattern[i] + center;
            if (!c.InBounds(map))
            {
                continue;
            }

            var thingList = c.GetThingList(map);
            foreach (var thing in thingList)
            {
                if (thing.def.size.x > 1 || thing.def.size.z > 1)
                {
                    if (returnedThings == null)
                    {
                        returnedThings = [];
                    }

                    if (!returnedThings.Add(thing))
                    {
                        continue;
                    }
                }

                yield return thing;
            }
        }
    }
}