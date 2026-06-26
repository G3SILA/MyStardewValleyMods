using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PetOutOfMyWay
{
    internal sealed class GenericModConfigMenu
    {
        private static ModEntry Entry;

        public static void Initialize(ModEntry modEntry)
        {
            Entry = modEntry;
        }
        public static void InitializeMenu()
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Entry.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: Entry.ModManifest,
                reset: () => Entry._config = new ModConfig(),
                save: () => Entry.Helper.WriteConfig(Entry._config)
            );

            
            configMenu.AddBoolOption(
                mod: Entry.ModManifest,
                name: () => "Enable Pet Out Of Way",
                tooltip: () => "Enable the entire mod",
                getValue: () => Entry._config.PetOutOfMyWayEnabled,
                setValue: value => Entry._config.PetOutOfMyWayEnabled = value
            );

            configMenu.AddNumberOption(
                mod: Entry.ModManifest,
                name: () => "Time Push Before Start Shaking",
                tooltip: () => "vanilla value: 300",
                getValue: () => Entry._config.TimeFarmerMustPushBeforeStartShaking,
                setValue: value => Entry._config.TimeFarmerMustPushBeforeStartShaking = value,
                min: 0,
                max: 600,
                interval: 10
            );

            configMenu.AddNumberOption(
                mod: Entry.ModManifest,
                name: () => "Time Push Before Pass Through",
                tooltip: () => "vanilla value: 750",
                getValue: () => Entry._config.TimeFarmerMustPushBeforePassingThrough,
                setValue: value => Entry._config.TimeFarmerMustPushBeforePassingThrough = value,
                min: 0,
                max: 1000,
                interval: 10
            );

        }
    }
}
