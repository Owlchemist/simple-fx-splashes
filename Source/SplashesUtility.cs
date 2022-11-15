using Verse;
using RimWorld;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace SimpleFxSplashes
{
	public static class SplashesUtility
	{
		public static HashSet<ushort> hardTerrains;
		public static Dictionary<int, Vector3[]> hardGrids = new Dictionary<int, Vector3[]>();
		public static Vector3[] activeMapHardGrid;
		static FastRandom fastRandom = new FastRandom();
		public static FleckSystem fleckSystemCache;
		static float cachedAltitude;
		public static int splashRate = 40, arrayChunks = 0, chunkIndex = 0, adjustedSplashRate = 40, activeMapID = -1;
		//public const int splashRateMax = 50, splashRateMin = 30;
		const int chunkSize = 1000;

		//Happens once on game start, goes through the database to find defs it think is hard and rain would bounce off of
		public static void Setup()
		{
			List<ushort> workingList = new List<ushort>();
			List<string> report = new List<string>();
			
			//Go through every terrain in the game
			foreach (TerrainDef terrainDef in DefDatabase<TerrainDef>.AllDefsListForReading)
			{
				//Does it have a cost?
				if (terrainDef.costList != null)
				{
					//Look through the costs
					foreach (ThingDefCountClass thingDefCountClass in terrainDef.costList)
					{
						//See if any of them are metallatic or stony, which we define as hard surfaces that rain would splash off of
						if (thingDefCountClass.thingDef?.stuffProps?.categories?.Any(x => x == ResourceBank.StuffCategoryDefOf.Metallic || x == ResourceBank.StuffCategoryDefOf.Stony) ?? false)
						{
							workingList.Add(terrainDef.index);
							report.Add(terrainDef.label);
							break;
						}
					}
				}
				else if (terrainDef.defName.Contains("_Rough"))
				{
					workingList.Add(terrainDef.index);
					report.Add(terrainDef.label);
				}
			}
			hardTerrains = workingList.ToHashSet();
			if (Prefs.DevMode) Log.Message("[Simple FX: Splashes] The following terrains have been defined as being hard:\n - " + string.Join("\n - ", report));

			cachedAltitude = ResourceBank.FleckDefOf.Owl_Splash.altitudeLayer.AltitudeFor(ResourceBank.FleckDefOf.Owl_Splash.altitudeLayerIncOffset);
		}

		public static void ProcessSplashes(Map map)
		{
			if (fastRandom.NextBool() && fastRandom.NextBool() && activeMapHardGrid != null) //This looks dumb, but it's gating more complex code behind 2 ultra-fast random bool checks.
			{
				if (fleckSystemCache == null) Find.CurrentMap.flecks.systems.TryGetValue(ResourceBank.FleckDefOf.Owl_Splash.fleckSystemClass, out fleckSystemCache);

				//Chunk start
				int chunkStart = (int)(chunkIndex * chunkSize);
				//Chunk end
				int chunkEnd = System.Math.Min(activeMapHardGrid.Length, (int)((chunkIndex * chunkSize) + chunkSize));

				for (int i = chunkStart; i < chunkEnd; ++i)
				{
					if (fastRandom.Next(adjustedSplashRate) == 0)
					{
						FleckCreationData dataStatic = FleckMaker.GetDataStatic(activeMapHardGrid[i], map, ResourceBank.FleckDefOf.Owl_Splash, ModSettings_SimpleFxSplashes.sizeMultiplier);
						dataStatic.spawnPosition.y = cachedAltitude;
						fleckSystemCache.CreateFleck(dataStatic);
					}
				}
				if (++chunkIndex == arrayChunks) chunkIndex = 0;
			}
		}

		public static void RebuildCache(Map map)
		{
			//First, ensure the key is set
			if (!hardGrids.ContainsKey(map.uniqueID)) hardGrids.Add(map.uniqueID, null);

			//Generate a working list
			List<Vector3> workingList = new List<Vector3>();
			var length = map.info.NumCells;
			fastRandom.Reinitialise(map.uniqueID); //Keep random cells consisent
			for (int i = 0; i < length; i++)
            {
				//Fetch the def cell by cell
                TerrainDef terrainDef = map.terrainGrid.topGrid[i];
				//The cell must be a valid def, not roofed, and not fogged
                if (hardTerrains.Contains(terrainDef.index) && 
					(!terrainDef.natural || fastRandom.Next(100) < (ModSettings_SimpleFxSplashes.natureFilter * 100)) && 
					map.roofGrid.roofGrid[i] == null && 
					!map.fogGrid.fogGrid[i]) workingList.Add(map.cellIndices.IndexToCell(i).ToVector3().RandomOffset());
            }

			//Record
			hardGrids[map.uniqueID] = workingList.ToArray();

			//Debug
			if (Prefs.DevMode) Log.Message("[Simple FX: Splashes] Splash grid build with " + workingList.Count().ToString() + " cells.");

			SetActiveGrid(map);
		}

		public static void UpdateCache(Map map, IntVec3 c, TerrainDef def = null)
		{
			fastRandom.Reinitialise(map.uniqueID); //Make sure the vectors match
			Vector3 vector = c.ToVector3().RandomOffset();
			if (hardGrids.TryGetValue(map?.uniqueID ?? -1, out Vector3[] hardGrid))
			{
				//Filter out this cell
				var workingList = hardGrid.Where(x => x != vector).ToList();
				//Add the new cell if relevant
				if (def == null) def = map.terrainGrid.TerrainAt(map.cellIndices.CellToIndex(c));
				if (hardTerrains.Contains(def.index)) workingList.Add(vector);
				//Push the array back out
				hardGrids[map.uniqueID] = workingList.ToArray();
				SetActiveGrid(map);
			}
		}

		static Vector3 RandomOffset(this Vector3 vector)
		{
			return new Vector3(vector.x + ((fastRandom.Next(100) - 50) / 100f), vector.y, vector.z + ((fastRandom.Next(100) - 50) / 100f));
		}

		public static void SetActiveGrid(Map map)
		{
			//Update the active grid.
			if (Find.CurrentMap?.uniqueID == map.uniqueID && hardGrids.TryGetValue(map.uniqueID, out activeMapHardGrid))
			{
				arrayChunks = (int)System.Math.Ceiling(activeMapHardGrid.Length / (float)chunkSize);
				chunkIndex = 0;
				//Adjusted splash rate
				adjustedSplashRate = (int)System.Math.Ceiling((splashRate * ModSettings_SimpleFxSplashes.splashRarity) / arrayChunks);
				activeMapID = map.uniqueID;
			}
		}
	}
}
