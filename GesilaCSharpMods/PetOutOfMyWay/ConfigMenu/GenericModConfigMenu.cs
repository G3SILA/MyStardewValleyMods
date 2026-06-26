
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
                name: () => Entry.Helper.Translation.Get("config.enabled.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.enabled.tooltip"),
                getValue: () => Entry._config.PetOutOfMyWayEnabled,
                setValue: value => Entry._config.PetOutOfMyWayEnabled = value
            );

            configMenu.AddNumberOption(
                mod: Entry.ModManifest,
                name: () => Entry.Helper.Translation.Get("config.push-before-shaking.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.push-before-shaking.tooltip"),
                getValue: () => Entry._config.TimeFarmerMustPushBeforeStartShaking,
                setValue: value => Entry._config.TimeFarmerMustPushBeforeStartShaking = value,
                min: 0,
                max: 600,
                interval: 50
            );

            configMenu.AddNumberOption(
                mod: Entry.ModManifest,
                name: () => Entry.Helper.Translation.Get("config.push-before-pass-through.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.push-before-pass-through.tooltip"),
                getValue: () => Entry._config.TimeFarmerMustPushBeforePassingThrough,
                setValue: value => Entry._config.TimeFarmerMustPushBeforePassingThrough = value,
                min: 0,
                max: 1000,
                interval: 50
            );

        }
    }
}
