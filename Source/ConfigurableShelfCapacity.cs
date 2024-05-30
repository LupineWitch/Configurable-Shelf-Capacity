using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;
using Verse.Noise;

namespace LupineWitch.ConfigurableShelfCapacity
{
    [StaticConstructorOnStartup]
    public static class ConfigurableShelfCapacity
    {

        static ConfigurableShelfCapacity()
        {
            IEnumerable<ThingDef> allDefs = DefDatabase<ThingDef>.AllDefs;
            if (allDefs == null)
            {
                Log.Error($"{nameof(ConfigurableShelfCapacity)}: Can't get defs from def database!");
                return;
            }

            IEnumerable<ThingDef> foundDefs = allDefs.Where(def => def.thingClass == typeof(Building_Storage));
#if DEBUG
            Log.Message($"{nameof(ConfigurableShelfCapacity)} Found storage defs: {foundDefs?.Count() ?? -1}");
            Harmony.DEBUG = true;
#endif
            ConfigurableShelfCapacitySettings.InitDefCollection(foundDefs);
            ConfigurableShelfCapacitySettings.ApplySettings();
            ConfigurableShelfCapacityMod.SetBufferValues();

            var harmony = new Harmony("patch.shelfutils.lupinewitch.mods");
            harmony.PatchAll();
        }
    }

    public class ConfigurableShelfCapacityMod : Mod
    {
        private static Vector2 scrollPos;
        public ConfigurableShelfCapacitySettings Settings;
        private static Dictionary<string, string> defNameCapacityBuffer = new Dictionary<string, string>();

        public ConfigurableShelfCapacityMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ConfigurableShelfCapacitySettings>();
        }

        public static void SetBufferValues()
        {
            foreach (var setting in ConfigurableShelfCapacitySettings.SettingsDictionary)
                defNameCapacityBuffer.Add(setting.Key, setting.Value.ToString());
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            string towerThresholdBuffer = ConfigurableShelfCapacitySettings.SplitVisualStackCount.ToString();

            Rect bigRect =  new Rect( 0f, 0f, 0.9f * inRect.width, inRect.height * 100f);
            Rect smallRect = new Rect(0f, 50f, inRect.width, 0.95f * inRect.height);

            Listing_Standard settingsScrolLView = new Listing_Standard();
            settingsScrolLView.ColumnWidth = 0.8f * inRect.width;
            
            settingsScrolLView.Begin(bigRect.AtZero());
            Widgets.BeginScrollView(smallRect, ref scrollPos, bigRect, true);
            
            settingsScrolLView.Label(string.Format("Items should tower up to {0} stacks per cell (minimum 2)", ConfigurableShelfCapacitySettings.SplitVisualStackCount));
            settingsScrolLView.IntEntry(ref ConfigurableShelfCapacitySettings.SplitVisualStackCount, ref towerThresholdBuffer);

            settingsScrolLView.Label(string.Format("Minimum capacity is {0}, maximum capacity is {1}", ConfigurableShelfCapacitySettings.MIN_SHELF_CAPACITY, ConfigurableShelfCapacitySettings.MAX_SHELF_CAPACITY));

            settingsScrolLView.Gap();

            foreach (var entry in ConfigurableShelfCapacitySettings.StorageBuildings)
            {
                settingsScrolLView.Label(entry.label);

                if (!defNameCapacityBuffer.TryGetValue(entry.defName, out string shelfBuffer))
                {
                    Log.Warning($"{nameof(ConfigurableShelfCapacity)}: No key {entry.defName} in collection {nameof(defNameCapacityBuffer)}. Creating default entry with value 1...");
                    defNameCapacityBuffer.Add(entry.defName, "1");
                }

                int settingReference;

                if (ConfigurableShelfCapacitySettings.SettingsDictionary.ContainsKey(entry.defName))
                    settingReference = ConfigurableShelfCapacitySettings.SettingsDictionary[entry.defName];
                else
                {
                    Log.Error($"Key mismatch between {nameof(ConfigurableShelfCapacitySettings.SettingsDictionary)} & {nameof(ConfigurableShelfCapacitySettings.StorageBuildings)}, no key {entry.defName} inside {nameof(ConfigurableShelfCapacitySettings.SettingsDictionary)}");
                    continue;
                }

                settingsScrolLView.IntEntry(ref settingReference, ref shelfBuffer);
                defNameCapacityBuffer[entry.defName] = shelfBuffer;
                i++;
            }

            Widgets.EndScrollView();
            settingsScrolLView.End();

            ConfigurableShelfCapacitySettings.SplitVisualStackCount = ConfigurableShelfCapacitySettings.SplitVisualStackCount.Clamp(2, int.MaxValue);
            foreach (var valueBuffer in defNameCapacityBuffer)
            {
                int clampedValue = Mathf.Clamp(int.Parse(valueBuffer.Value), ConfigurableShelfCapacitySettings.MIN_SHELF_CAPACITY, ConfigurableShelfCapacitySettings.MAX_SHELF_CAPACITY);
                ConfigurableShelfCapacitySettings.SettingsDictionary[valueBuffer.Key] = clampedValue;
            }
        }

        public override string SettingsCategory()
        {
            return "Configurable Shelf Capacity";
        }

        public override void WriteSettings()
        {
            ConfigurableShelfCapacitySettings.ApplySettings();
            base.WriteSettings();
        }
    }
}
