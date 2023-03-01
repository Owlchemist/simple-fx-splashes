using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.Rendering;
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

    //The main process loop is attached to the weather ticker
	[HarmonyPatch(typeof(Map), nameof(Map.MapPostTick))]
	public class Patch_MapPostTick 
	{
		static public void Postfix(Map __instance)
		{
            if (activeMapID == __instance.uniqueID && 
			__instance.weatherManager.curWeather.rainRate > 0f && 
			__instance.weatherManager.curWeather.snowRate == 0f) SplashesUtility.ProcessSplashes(__instance);
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
	[HarmonyPatch(typeof(Game), nameof(Game.DeinitAndRemoveMap_NewTemp))]
	public class Patch_DeinitAndRemoveMap
	{
		static public void Postfix(Map map)
		{
			if (map != null)
			{
				hardGrids.Remove(map.uniqueID);
				SetActiveGrid(Find.CurrentMap);
			}
		}
	}

    //Optimized weather overlays
	[HarmonyPatch(typeof(SkyOverlay), nameof(SkyOverlay.DrawOverlay))]
	[HarmonyPriority(Priority.Last)]
    public class Patch_ReplaceOverlay
	{
        static Matrix4x4 worldMatrix = default(Matrix4x4), screenMatrix = default(Matrix4x4);
        static int mapID = -1;
        static bool dirty;
		static bool Prepare()
		{
			return ModSettings_SimpleFxSplashes.optimizeOverlay;
		}
		static public bool Prefix(SkyOverlay __instance, Map map)
		{
            dirty = mapID != map.uniqueID;
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
            return false;
        }
    }

	//Flush the cache on reloads
	[HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
	class ResetCacheTriggers
	{
		static void Postfix()
		{
			ResetCache();
		}
	}
}