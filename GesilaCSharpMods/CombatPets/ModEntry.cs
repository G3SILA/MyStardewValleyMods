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
        public ModConfig _config = null!;

        private MultiplayerService Multiplayer = null!;
        private PetRegister _petRegister = null!;

        // current following managers
        private List<PetManager> _petManagers = new();

        public override void Entry(IModHelper helper)
        {
            this._config = this.Helper.ReadConfig<ModConfig>();
            PetDataService Data = new(ModManifest, Monitor);
            Multiplayer = new MultiplayerService(this);

            _petRegister = new PetRegister(Monitor, helper, () => _config, Data);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.World.NpcListChanged += this.OnNpcListChanged;
            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            helper.Events.Multiplayer.ModMessageReceived += Multiplayer.OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += Multiplayer.OnPeerConnected;
            Multiplayer.ToggleFollowRequested += HandleToggleFollowRequest;
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

            Vector2 tileLocation = e.Cursor.GrabTile;
            Rectangle tileRect = new Rectangle((int)tileLocation.X * Game1.tileSize, (int)tileLocation.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize);

            PetManager? manager = _petRegister.FindAtTile(Game1.currentLocation, tileRect);

            if (manager == null) return;

            if (Game1.currentLocation is MineShaft)
            {
                Game1.showRedMessage(Helper.Translation.Get("follow.disabled-in-mines", new { petName = manager.pet.name }));
                return;
            }

            string petId = manager.PetId;
            if (Context.IsMainPlayer)
                HandleToggleFollowRequest(Game1.player.UniqueMultiplayerID, petId);
            else
                Multiplayer.SendToggleFollowRequest(petId);     

        }

        private void addToFollow(Pet pet, Farmer owner, bool showFeedback = true)
        {
            
            var manager = _petRegister.getManager(pet);

            manager.AssignOwner(owner.UniqueMultiplayerID);
            _petManagers.Add(manager);
            ++following;
            if (showFeedback)
            {
                pet.playContentSound();
                pet.doEmote(56);
            }
            
        }

        private void removeFromFollow(Pet pet)
        {
            var manager = _petRegister.getManager(pet);
            manager.StopFollowing();
            _petManagers.Remove(manager);
            --following;
        }


        private void HandleToggleFollowRequest(long requesterId, string petId)
        {
            if (!Context.IsMainPlayer) return;

            Farmer? requester = Game1.getOnlineFarmers().FirstOrDefault(farmer => farmer.UniqueMultiplayerID == requesterId);
            PetManager? manager = _petRegister.getManager(petId);

            ToggleFollowResultMessage result;
            if (requester is null || manager is null)
            {
                result = ToggleFollowResultMessage.Failure(petId, manager?.pet.Name ?? "", "not-found");
            }
            else if (manager.IsFollowing && manager.OwnerId != requesterId)
            {
                result = ToggleFollowResultMessage.Failure(petId, manager.pet.Name, "owned");
            }
            else if (manager.IsFollowing)
            {
                removeFromFollow(manager.pet);
                result = ToggleFollowResultMessage.SuccessToggle(manager, isFollowing: false);
            }
            else if (following >= _config.MaxNumberFollowers)
            {
                result = ToggleFollowResultMessage.Failure(petId, manager.pet.Name, "capacity");
            }
            else
            {
                addToFollow(manager.pet, requester);
                result = ToggleFollowResultMessage.SuccessToggle(manager, isFollowing: true);
            }

            if (requesterId == Game1.player.UniqueMultiplayerID)
                Multiplayer.ShowToggleFollowResult(result);
            else
                Multiplayer.SendToggleFollowResult(requesterId, result);
        }

    }
}
