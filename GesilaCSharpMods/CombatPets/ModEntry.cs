using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;

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
            PetDataService Data = new(ModManifest, Monitor);
            _petRegister = new PetRegister(Monitor, helper, () => _config, Data);

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
                    addToFollow(pet, Game1.player, false); // for now, only allow main player to add pets to follow
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
            // TODO: must be updated for multiplayer, as the pet may not be at the same position as main player
            if (!Context.IsWorldReady || !Context.IsMainPlayer)
                return;

            Pet? removed = _petRegister.IsPetRemoved(sender, e);
            if (removed != null)
            {
                PetManager? manager = _petRegister.getManager(removed);

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
            if (!Context.IsWorldReady
                || e.Button != _config.TogglePetFollowingKeybind
                || Game1.currentLocation is null)
            {
                return;
            }

            _petRegister.ApplyToAllPets(pet =>
            {
                Vector2 tileLocation = e.Cursor.GrabTile;
                Rectangle tileRect = new Rectangle((int)tileLocation.X * Game1.tileSize, (int)tileLocation.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
                if (pet.GetBoundingBox().Intersects(tileRect))
                {
                    if (Game1.currentLocation is MineShaft)
                    {
                        Game1.showRedMessage(Helper.Translation.Get("follow.disabled-in-mines", new { petName = pet.name }));
                        return;
                    }
                    if (_petManagers.Contains(_petRegister.getManager(pet)))
                    {
                        removeFromFollow(pet);
                    } else
                    {
                        addToFollow(pet, Game1.player); // for now, only allow main player to add pets to follow
                    }
                }
            });
        }

        private void addToFollow(Pet pet, Farmer owner, bool showFeedback = true)
        {
            int max = _config.MaxNumberFollowers;
            if (following < max)
            {
                var manager = _petRegister.getManager(pet);
                if (manager.IsFollowing is true)
                {
                    if (showFeedback)
                    {
                        Game1.showRedMessage(Helper.Translation.Get("follow.already-following", new { petName = pet.name, farmerName = manager.GetOwner().name }));
                    }
                    return;
                }

                manager.AssignOwner(owner.UniqueMultiplayerID);
                _petManagers.Add(manager);
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
            var manager = _petRegister.getManager(pet);
            manager.StopFollowing();
            _petManagers.Remove(manager);
            Game1.showGlobalMessage(Helper.Translation.Get("follow.stopped",new { petName = pet.name }));
            --following;
        }

    }
}
