using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Reflection;
using static SimpleFxSplashes.SplashesUtility;
 
namespace SimpleFxSplashes
{
    //Build the terrain cache
	[HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
	public class Patch_FinalizeInit
	{
		static public void Postfix(Map __instance)
		{
            RebuildCache(__instance);
		}
    }

    //When the map changes, change the fleck system cache
	[HarmonyPatch(typeof(MapInterface), nameof(MapInterface.Notify_SwitchedMap))]
    public class Patch_Notify_SwitchedMap
	{
		static public void Postfix()
		{
            Map map = Find.CurrentMap;
            if (map != null)
            {
                map.flecks.systems.TryGetValue(ResourceBank.FleckDefOf.Owl_Splash.fleckSystemClass, out fleckSystemCache);
				SetActiveGrid(map);
            }
        }
    }

    //Build the terrain cache
	[HarmonyPatch(typeof(WeatherDecider), nameof(WeatherDecider.WeatherDeciderTick))]
	public class Patch_WeatherDeciderTick
	{
        //static HashSet<int> rainingMaps = new HashSet<int>();
        //static int ticker = 299;
		static public void Postfix(Map ___map)
		{
			/*
            if (++ticker == 300) 
            {
                ticker = 0;
                if (___map.weatherManager.RainRate > 0.2f) rainingMaps.Add(___map.uniqueID);
				else rainingMaps.Remove(___map.uniqueID);
            }
			*/
            if (___map.weatherManager.curWeather.rainRate > 0 && activeMapID == ___map.uniqueID) SplashesUtility.ProcessSplashes(___map);
        }
    }

    //If there's a terrain change...
	[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.SetTerrain))]
    public class Patch_SetTerrain { static public void Postfix(TerrainGrid __instance, IntVec3 c, TerrainDef newTerr) { UpdateCache(__instance.map, c, newTerr); } }

    //If there's a terrain removal...
	[HarmonyPatch(typeof(TerrainGrid), nameof(TerrainGrid.RemoveTopLayer))]
    public class Patch_RemoveTopLayer { static public void Postfix(TerrainGrid __instance, IntVec3 c) { UpdateCache(__instance.map, c, null); } }

    //If there's a roof change...
	[HarmonyPatch(typeof(RoofGrid), nameof(RoofGrid.SetRoof))]
    public class Patch_SetRoof { static public void Postfix(RoofGrid __instance, IntVec3 c) { UpdateCache(__instance.map, c, null); } }

    //Update cache if a map is removed
	[HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap))]
	public class Patch_DeinitAndRemoveMap
	{
		static public void Postfix(Map __instance)
		{
			if (__instance != null)
			{
				hardGrids.Remove(__instance.uniqueID);
                hardGrids.TryGetValue(Find.CurrentMap?.uniqueID ?? -1, out activeMapHardGrid);
			}
		}
	}

    //Optimized weather overlays
	[HarmonyPatch(typeof(SkyOverlay), nameof(SkyOverlay.DrawOverlay))]
    public class Patch_ReplaceOverlay
	{
        static Matrix4x4 worldMatrix = default(Matrix4x4);
        static Matrix4x4 screenMatrix = default(Matrix4x4);
        static int mapID = -1;
        static bool dirty;
		static public bool Prefix(SkyOverlay __instance, Map map)
		{
            if (!ModSettings_SimpleFxSplashes.optimizeOverlay) return true;
            if (mapID != map.uniqueID) dirty = true;
            if (!object.ReferenceEquals(__instance.worldOverlayMat, null))
			{
                if (dirty)
                {
				    Vector3 position = map.Center.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather);
                    worldMatrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
                    mapID = map.uniqueID;
                }

                Graphics.Internal_DrawMesh_Injected
				(
					MeshPool.wholeMapPlane, //Mesh
					0, //SubMeshIndex
					ref worldMatrix, //Matrix
					__instance.worldOverlayMat, //Material
					0, //Layer
					null, //Camera
					null, //MaterialPropertiesBlock
					ShadowCastingMode.Off, //castShadows
					false, //recieveShadows
					null, //probeAnchor
					LightProbeUsage.Off, //LightProbeUseage
					null //LightProbeProxyVolume
				);
			}
            if (!object.ReferenceEquals(__instance.screenOverlayMat, null))
			{
                Camera camera = Find.Camera;
				float num = camera.orthographicSize * 2f;
				Vector3 s = new Vector3(num * camera.aspect, 1f, num);
				Vector3 position2 = camera.transform.position;
				position2.y = 29.04054054f;
				
				screenMatrix.SetTRS(position2, Quaternion.identity, s);

                Graphics.Internal_DrawMesh_Injected
				(
					MeshPool.plane10, //Mesh
					0, //SubMeshIndex
					ref screenMatrix, //Matrix
					__instance.screenOverlayMat, //Material
					0, //Layer
					null, //Camera
					null, //MaterialPropertiesBlock
					ShadowCastingMode.Off, //castShadows
					false, //recieveShadows
					null, //probeAnchor
					LightProbeUsage.Off, //LightProbeUseage
					null //LightProbeProxyVolume
				);
			}
            dirty = false;
            return false;
        }

		[HarmonyPatch]
		class ResetCacheTriggers
		{
			static IEnumerable<MethodBase> TargetMethods()
			{
				//If options are changed..
				yield return AccessTools.Method(typeof(Game), nameof(Game.LoadGame));
				//If colonist portrait is being dragged n' dropped...
				yield return AccessTools.Method(typeof(Game), nameof(Game.InitNewGame));
			}

			static void Prefix()
			{
				ResetCache();
			}
		}
    }
}