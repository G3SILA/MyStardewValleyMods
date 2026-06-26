using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using System;

namespace PetOutOfMyWay
{
    internal sealed class ModEntry : Mod
    {
        private ModConfig _config = new();
        public override void Entry(IModHelper helper)
        {
            this.Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            PetPushPatch.Initialize(Monitor, _config);

            var harmony = new Harmony(this.ModManifest.UniqueID);

            if (_config.PetOutOfMyWayEnabled)
            {

                harmony.Patch(
                   original: AccessTools.Method(typeof(StardewValley.Characters.Pet), nameof(StardewValley.Characters.Pet.getTimeFarmerMustPushBeforePassingThrough)),
                   postfix: new HarmonyMethod(typeof(PetPushPatch), nameof(PetPushPatch.getTimeFarmerMustPushBeforePassingThrough_Postfix))
                );

                harmony.Patch(
                   original: AccessTools.Method(typeof(StardewValley.Characters.Pet), nameof(StardewValley.Characters.Pet.getTimeFarmerMustPushBeforeStartShaking)),
                   postfix: new HarmonyMethod(typeof(PetPushPatch), nameof(PetPushPatch.getTimeFarmerMustPushBeforeStartShaking_Postfix))
                );
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {

        }

    }
}