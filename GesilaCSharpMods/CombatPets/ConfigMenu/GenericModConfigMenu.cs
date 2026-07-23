
using StardewModdingAPI;

namespace CombatPets
{
    internal sealed class GenericModConfigMenu
    {
        private static ModEntry Entry;
        private static IGenericModConfigMenuApi? configMenu;

        public static void Initialize(ModEntry modEntry)
        {
            Entry = modEntry;
            // get Generic Mod Config Menu's API (if it's installed)
            configMenu = Entry.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        }
        public static void InitializeMenu()
        {
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: Entry.ModManifest,
                reset: () => Entry._config = new ModConfig(),
                save: () => Entry.Helper.WriteConfig(Entry._config)
            );

            FollowingMenu();
            PathFindingMenu();
            CombatMenu();

        }

        private static void FollowingMenu()
        {
            configMenu.AddSectionTitle(
                mod: Entry.ModManifest,
                text: () => Entry.Helper.Translation.Get("config.following.section")
            );

            configMenu.AddBoolOption(
                mod: Entry.ModManifest,
                getValue: () => Entry._config.EnablePetFollowing,
                setValue: value => Entry._config.EnablePetFollowing = value,
                name: () => Entry.Helper.Translation.Get("config.enable-pet-following.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.enable-pet-following.tooltip")
            );

            configMenu.AddNumberOption(
                mod: Entry.ModManifest,
                getValue: () => Entry._config.MaxNumberFollowers,
                setValue: value => Entry._config.MaxNumberFollowers = value,
                name: () => Entry.Helper.Translation.Get("config.max-number-followers.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.max-number-followers.tooltip"),
                min: 0,
                max: 10,
                interval: 1
            );

            configMenu.AddNumberOption(
                mod: Entry.ModManifest,
                getValue: () => Entry._config.FollowDistance,
                setValue: value => Entry._config.FollowDistance = value,
                name: () => Entry.Helper.Translation.Get("config.follow-distance.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.follow-distance.tooltip"),
                min: 1,
                max: 10,
                interval: 1
            );

            configMenu.AddNumberOption(
                mod: Entry.ModManifest,
                getValue: () => Entry._config.AddedFollowSpeed,
                setValue: value => Entry._config.AddedFollowSpeed = value,
                name: () => Entry.Helper.Translation.Get("config.added-follow-speed.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.added-follow-speed.tooltip"),
                min: 0,
                max: 10,
                interval: 1
            );

            configMenu.AddBoolOption(
                mod: Entry.ModManifest,
                getValue: () => Entry._config.SoundOnJumpPet,
                setValue: value => Entry._config.SoundOnJumpPet = value,
                name: () => Entry.Helper.Translation.Get("config.sound-on-jump-pet.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.sound-on-jump-pet.tooltip")
            );

        }

        private static void PathFindingMenu()
        {
            configMenu.AddSectionTitle(
                mod: Entry.ModManifest,
                text: () => Entry.Helper.Translation.Get("config.pathfinding.section")
            );

            configMenu.AddNumberOption(
                mod: Entry.ModManifest,
                getValue: () => Entry._config.TimeToWarpWhenNoPathFound,
                setValue: value => Entry._config.TimeToWarpWhenNoPathFound = value,
                name: () => Entry.Helper.Translation.Get("config.time-to-warp-no-path.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.time-to-warp-no-path.tooltip"),
                min: 0,
                max: 600,
                interval: 30
            );
        }

        private static void CombatMenu()
        {
            configMenu.AddSectionTitle(
                mod: Entry.ModManifest,
                text: () => Entry.Helper.Translation.Get("config.combat.section")
            );

            configMenu.AddBoolOption(
                mod: Entry.ModManifest,
                getValue: () => Entry._config.EnableCombat,
                setValue: value => Entry._config.EnableCombat = value,
                name: () => Entry.Helper.Translation.Get("config.enable-combat.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.enable-combat.tooltip")
            );

            configMenu.AddTextOption(
                mod: Entry.ModManifest,
                getValue: () => Entry._config.PetStrength.ToString(),
                setValue: value => Entry._config.PetStrength = Enum.Parse<PetStrength>(value),
                name: () => Entry.Helper.Translation.Get("config.pet-strength.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.pet-strength.tooltip"),
                allowedValues: Enum.GetNames<PetStrength>(),
                formatAllowedValue: value => value switch
                {
                    "Helpful" => Entry.Helper.Translation.Get("config.pet-strength.helpful"),
                    "Normal" => Entry.Helper.Translation.Get("config.pet-strength.normal"),
                    "Overpowered" => Entry.Helper.Translation.Get("config.pet-strength.overpowered"),
                    _ => value
                }
            );

            configMenu.AddTextOption(
                mod: Entry.ModManifest,
                getValue: () => Entry._config.ShowHealthBar.ToString(),
                setValue: value => Entry._config.ShowHealthBar = Enum.Parse<ShowHealthBar>(value),
                name: () => Entry.Helper.Translation.Get("config.show-health-bar.name"),
                tooltip: () => Entry.Helper.Translation.Get("config.show-health-bar.tooltip"),
                allowedValues: Enum.GetNames<ShowHealthBar>(),
                formatAllowedValue: value => value switch
                {
                    "Always" => Entry.Helper.Translation.Get("config.show-health-bar.always"),
                    "InCombat" => Entry.Helper.Translation.Get("config.show-health-bar.in-combat"),
                    "Never" => Entry.Helper.Translation.Get("config.show-health-bar.never"),
                    _ => value
                }
            );
        }
    }
}
