using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace CombatPets
{
    internal sealed class ModEntry : Mod
    {
        private ModConfig _config = new();
        private PetFollowManager _petFollowManager;
        
        public override void Entry(IModHelper helper)
        {
            this._config = this.Helper.ReadConfig<ModConfig>();
            _petFollowManager = new PetFollowManager(Monitor, _config);

            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.World.NpcListChanged += this.OnNpcListChanged;
        }

        

        // initialize mod
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            _petFollowManager.OnDayStarted(sender, e); 

        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            _petFollowManager.OnWarped(sender, e);

        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            _petFollowManager.OnUpdateTicked(sender, e);
        }

        private void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
        {
           _petFollowManager.OnNpcListChanged(sender, e);

        }
    }
}
