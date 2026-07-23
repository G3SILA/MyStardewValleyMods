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
        public ModConfig _config;

        private PetRegister _petRegister;
        private List<PetManager> _petManagers = new();

        public override void Entry(IModHelper helper)
        {
            this._config = this.Helper.ReadConfig<ModConfig>();
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
            int number = _config.MaxNumberFollowers;
    
            // restart everyday
            _petManagers.Clear();
            Utility.getAllPets().ForEach(pet =>
            {
                if (number > 0)
                {
                    var petManager = new PetManager(Monitor, () => _config, this.Helper, pet);
                    _petManagers.Add(petManager);
                    --number;
                }
            });
            ApplyToAllPetManagers(manager => manager.OnDayStarted(sender, e));
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!Context.IsWorldReady || _petManagers is null)
                return;
            ApplyToAllPetManagers(manager => manager.OnWarped(sender, e)); 

        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            ApplyToAllPetManagers(manager => manager.OnUpdateTicked(sender, e));
        }

        public void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
        {
            Pet? removed = _petRegister.IsPetRemoved(sender, e);
            if (removed != null)
            {
                PetManager? manager = _petManagers.FirstOrDefault(manager => manager.pet.name == removed.name);

                if (manager != null)
                {
                    _petManagers.Remove(manager);
                }
            }
        }

        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (!Context.IsWorldReady || _petManagers is null)
                return;
            ApplyToAllPetManagers(manager => manager.OnRendered(sender, e));
        }

        private void ApplyToAllPetManagers(Action<PetManager> action)
        {
            foreach (var manager in _petManagers)
            {
                action(manager);
            }
        }
    }
}
