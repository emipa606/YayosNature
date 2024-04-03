using System.Collections.Generic;
using LudeonTK;
using Verse;

namespace YayoNature;

public static class DebugActionsMisc
{
    [DebugAction("Yayo's Nature", allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void ChangeBiome()
    {
        var list = new List<DebugMenuOption>
        {
            new DebugMenuOption("start.NextBiome", DebugMenuOptionMode.Action,
                delegate { dataUtility.GetData(Current.Game.CurrentMap).startChange(); })
        };
        foreach (var b in core.ar_b)
        {
            list.Add(new DebugMenuOption($"start.{b.label}", DebugMenuOptionMode.Action,
                delegate { dataUtility.GetData(Current.Game.CurrentMap).startChange(b); }));
        }

        foreach (var b in core.ar_b)
        {
            list.Add(new DebugMenuOption($"make.{b.label}(few minute)", DebugMenuOptionMode.Action,
                delegate { dataUtility.GetData(Current.Game.CurrentMap).debugChangeImmediately(b); }));
        }

        Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
    }


    [DebugAction("Yayo's Nature", allowedGameStates = AllowedGameStates.Playing)]
    private static void ResetPlanetForNewBiome()
    {
        core.resetPlanet(true);
    }
}