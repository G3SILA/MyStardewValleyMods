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
        private int following = 0; 
        public ModConfig _config;

        private PetRegister _petRegister;

        // current following managers
        private List<PetManager> _petManagers = new();

        public override void Entry(IModHelper helper)
        {
            this._config = this.Helper.ReadConfig<ModConfig>();
            _petRegister = new PetRegister(Monitor, helper, () => _config);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.World.NpcListChanged += this.OnNpcListChanged;
            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
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
            _petRegister.OnDayStarted(sender, e);

            // restart everyday
            following = 0;
            _petManagers.Clear();

            if (_config.FillUpTeamOnDayStarted)
            {
                _petRegister.getAllPetsInAllLocations().ForEach(pet =>
                {
                    addToFollow(pet, false);
                });
            }

            if (_config.WarpAllPetsBackToFarmHouseOnDayStarted)
            {
                _petRegister.ApplyToAllPets(pet =>
                {
                    Game1.warpCharacter(pet, "FarmHouse", Game1.player.TilePoint);
                });
            }

            ApplyToAllPetManagers(manager => manager.OnDayStarted(sender, e));
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            ApplyToAllPetManagers(manager => manager.OnWarped(sender, e)); 

        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            ApplyToAllPetManagers(manager => manager.OnUpdateTicked(sender, e));
        }

        private void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
        {
            Pet? removed = _petRegister.IsPetRemoved(sender, e);
            if (removed != null)
            {
                PetManager? manager = _petManagers.FirstOrDefault(manager => manager.pet.petId == removed.petId);

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

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            _petRegister.ApplyToAllPets(pet =>
            {
                Vector2 tileLocation = e.Cursor.GrabTile;
                Rectangle tileRect = new Rectangle((int)tileLocation.X * 64, (int)tileLocation.Y * 64, 64, 64);
                if (e.Button == _config.TogglePetFollowingKeybind && pet.GetBoundingBox().Intersects(tileRect))
                {
                    if (_petManagers.Contains(_petRegister.getManager(pet)))
                    {
                        removeFromFollow(pet);
                    } else
                    {
                        addToFollow(pet);
                    }
                }
            });
        }

        private void addToFollow(Pet pet, bool showFeedback = true)
        {
            int max = _config.MaxNumberFollowers;
            if (following < max)
            {
                _petManagers.Add(_petRegister.getManager(pet));
                ++following;
                if (showFeedback)
                {
                    Game1.showGlobalMessage(Helper.Translation.Get("follow.started", new { petName = pet.name }));
                    pet.playContentSound();
                    pet.doEmote(56);
                }
            } else
            {
                if (showFeedback)
                {
                    Game1.showRedMessage(Helper.Translation.Get("follow.capacity-reached"));
                }
            }
        }

        private void removeFromFollow(Pet pet)
        {
            _petManagers.Remove(_petRegister.getManager(pet));
            Game1.showGlobalMessage(Helper.Translation.Get("follow.stopped",new { petName = pet.name }));
            --following;
        }
    }
}
