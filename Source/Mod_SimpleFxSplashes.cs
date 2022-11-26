using Verse;
using HarmonyLib;
using UnityEngine;
using System;
using static SimpleFxSplashes.ModSettings_SimpleFxSplashes;
 
namespace SimpleFxSplashes
{
    public class Mod_SimpleFxSplashes : Mod
	{
		public Mod_SimpleFxSplashes(ModContentPack content) : base(content)
		{
			base.GetSettings<ModSettings_SimpleFxSplashes>();
			new Harmony(this.Content.PackageIdPlayerFacing).PatchAll();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard options = new Listing_Standard();
			options.Begin(inRect);
			options.CheckboxLabeled("SimpleFxSplashes.Settings.OptimizeOverlay".Translate(), ref optimizeOverlay, "SimpleFxSplashes.Settings.OptimizeOverlay.Desc".Translate());
			options.Gap();
			options.Label("SimpleFxSplashes.Settings.SizeMultiplier".Translate("1", "0.5", "2") + Math.Round(sizeMultiplier, 1), -1f, "SimpleFxSplashes.Settings.SizeMultiplier.Desc".Translate());
			sizeMultiplier = options.Slider(sizeMultiplier, 0.5f, 2f);
			options.Label("SimpleFxSplashes.Settings.SplashRarity".Translate("1", "0.5", "2") + Math.Round(splashRarity, 1), -1f, "SimpleFxSplashes.Settings.SplashRarity.Desc".Translate());
			splashRarity = options.Slider(splashRarity, 0.5f, 2f);
			options.Label("SimpleFxSplashes.Settings.FilterNature".Translate("0.1", "0", "1") + Math.Round(natureFilter, 2), -1f, "SimpleFxSplashes.Settings.FilterNature.Desc".Translate((Math.Round(natureFilter, 2) * 100).ToString()));
			natureFilter = options.Slider(natureFilter, 0f, 1f);
			options.End();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Simple FX: Splashes";
		}

		public override void WriteSettings()
		{
			base.WriteSettings();
			if (Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null)
			{
				foreach (var map in Find.Maps) SplashesUtility.RebuildCache(map);
			}
		}
	}
	public class ModSettings_SimpleFxSplashes : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Values.Look<bool>(ref optimizeOverlay, "optimizeOverlay", true);
			Scribe_Values.Look<float>(ref sizeMultiplier, "sizeMultiplier", 1f);
			Scribe_Values.Look<float>(ref splashRarity, "splashMultiplier", 1f);
			Scribe_Values.Look<float>(ref natureFilter, "natureFilter", 0.1f);
			base.ExposeData();
		}

		public static bool optimizeOverlay = true;
		public static float sizeMultiplier = 1f, splashRarity = 1f, natureFilter = 0.1f;
	}
}
