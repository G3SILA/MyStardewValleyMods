using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;

namespace CombatPets
{
    internal sealed class ModEntry : Mod
    {
        private ModConfig _config = new();
        private PetManager _petFollowManager;

        public override void Entry(IModHelper helper)
        {
            _petFollowManager = new PetManager(Monitor, () => _config, this.Helper);

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
