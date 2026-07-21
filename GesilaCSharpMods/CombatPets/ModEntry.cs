using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;

namespace CombatPets
{
    internal sealed class ModEntry : Mod
    {
        public ModConfig _config = new();

        private PetRegister _petRegister;
        private PetManager _petFollowManager;

        public override void Entry(IModHelper helper)
        {
            _petRegister = new PetRegister(Monitor);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.World.NpcListChanged += this.OnNpcListChanged;
            helper.Events.Display.Rendered += this.OnRendered;
        }

        // integration with Generic Mod Config Menu
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            GenericModConfigMenu.Initialize(this);
            GenericModConfigMenu.InitializeMenu();
        }


        // initialize mod
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            Pet pet = _petRegister.getFirstPet();
            _petFollowManager = new PetManager(Monitor, () => _config, this.Helper, pet); 
            _petFollowManager.OnDayStarted(sender, e); 

        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!Context.IsWorldReady || _petFollowManager is null)
                return;
            _petFollowManager.OnWarped(sender, e);

        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            _petFollowManager.OnUpdateTicked(sender, e);
        }

        public void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
        {
            _petRegister.OnNpcListChanged(sender, e);

            // is my pet still present? 
        }

        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (!Context.IsWorldReady || _petFollowManager is null)
                return;
            _petFollowManager.OnRendered(sender, e);
        }
    }
}
