using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace LupineWitch.ConfigurableShelfCapacity
{
    public class ConfigurableShelfCapacitySettings : ModSettings
    {
        public const int MAX_SHELF_CAPACITY = 80;
        public const int MIN_SHELF_CAPACITY = 1;

        public static int SplitVisualStackCount = 3;

        public static IReadOnlyCollection<ThingDef> StorageBuildings => storageDefs;
        public static Dictionary<string, int> SettingsDictionary = new Dictionary<string, int>();
        
        private static List<ThingDef> storageDefs = new List<ThingDef>();

        public static void InitDefCollection(IEnumerable<ThingDef> foundDefs)
        {

            if(foundDefs is null)
            {
                Log.Error("Failed to initialise ConfigurableShelfCapacity Mod! Reason: No valid storage building defs.");
                return;
            }
            if(storageDefs is null)
            {
                Log.Warning($"Error on initialise ConfigurableShelfCapacity Mod! {nameof(storageDefs)} is null");
                storageDefs = new List<ThingDef>();
                return;
            }
            if(SettingsDictionary is null)
            {
                Log.Warning($"Error on initialise ConfigurableShelfCapacity Mod! {nameof(SettingsDictionary)} is null");
                SettingsDictionary = new Dictionary<string, int>();
                return;
            }

            storageDefs.AddRange(foundDefs);
            var existingDefNames = storageDefs.Select(sd => sd.defName).ToArray();
            SettingsDictionary.RemoveAll(s => !existingDefNames.Contains(s.Key));

            foreach (var def in foundDefs)
            {
                if(SettingsDictionary.ContainsKey(def.defName))
                    continue;

                BuildingProperties buildingProperties = def.building;
                if(buildingProperties == null)
                {
                    Log.Error($"{nameof(ConfigurableShelfCapacitySettings)}:{nameof(buildingProperties)} is null for the {def.defName}");
                    continue;
                }

                SettingsDictionary.Add(def.defName, buildingProperties.maxItemsInCell);
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref SettingsDictionary, nameof(SettingsDictionary), LookMode.Value);
            Scribe_Values.Look(ref SplitVisualStackCount, "SplitVisualStackCount", 3);
            base.ExposeData();
        }

        public static void ApplySettings()
        {
            foreach(ThingDef storage in storageDefs)
            {
                if (SettingsDictionary.ContainsKey(storage.defName))
                    storage.building.maxItemsInCell = SettingsDictionary[storage.defName];
                else
                    Log.Warning($"[{nameof(ConfigurableShelfCapacityMod)}]:No ThingDef:{storage.defName} with thingClass:{nameof(Building_Storage)}");
            }
        }
    }
}
