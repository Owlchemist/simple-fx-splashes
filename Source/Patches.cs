using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using static SimpleFxSplashes.SplashesUtility;
 
namespace SimpleFxSplashes
{
    //Build the terrain cache
	[HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
	class Patch_Map_FinalizeInit
	{
		static void Postfix(Map __instance)
		{
            RebuildCache(__instance);
		}
    }

    //When the map changes, change the fleck system cache
	[HarmonyPatch(typeof(MapInterface), nameof(MapInterface.Notify_SwitchedMap))]
    class Patch_Notify_SwitchedMap
	{
		static void Postfix()
		{
            Map map = Find.CurrentMap;
            if (map != null)
            {
                map.flecks.systems.TryGetValue(ResourceBank.DefOf.Owl_Splash.fleckSystemClass, out fleckSystemCache);
				SetActiveGrid(map);
            }
        }
    }

    //The main process loop is attached to the weather ticker
	[HarmonyPatch(typeof(Map), nameof(Map.MapPreTick))]
	class Patch_Map_MapPreTick
	{
		static void Postfix(Map __instance)
		{
			var curWeather = __instance.weatherManager.curWeather;
            if (activeMapID == __instance.uniqueID && 
			curWeather.rainRate > 0f && 
			curWeather.snowRate == 0f) SplashesUtility.ProcessSplashes(__instance);
        }
    }

    //If there's a terrain change...
	[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.SetTerrain))]
    class Patch_TerrainGrid_SetTerrain { static void Postfix(TerrainGrid __instance, IntVec3 c, TerrainDef newTerr) { UpdateCache(__instance.map, c, newTerr); } }

    //If there's a terrain removal...
	[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.RemoveTopLayer))]
    class Patch_TerrainGrid_RemoveTopLayer { static void Postfix(TerrainGrid __instance, IntVec3 c) { UpdateCache(__instance.map, c, null); } }

    //If there's a roof change...
	[HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
    class Patch_RoofGrid_SetRoof { static void Postfix(RoofGrid __instance, IntVec3 c) { UpdateCache(__instance.map, c, null); } }

    //Update cache if a map is removed
	[HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap_NewTemp))]
	class Patch_Game_DeinitAndRemoveMap
	{
		static void Postfix(Map map)
		{
			if (map != null)
			{
				hardGrids.Remove(map.uniqueID);
				SetActiveGrid(Find.CurrentMap);
			}
		}
	}

	//Flush the cache on reloads
	[HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
	class Patch_World_FinalizeInit
	{
		static void Postfix()
		{
			ResetCache();
		}
	}
}