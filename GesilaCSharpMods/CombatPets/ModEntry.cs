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
        public ModConfig _config = null!;

        private MultiplayerService Multiplayer = null!;
        private PetRegister _petRegister = null!;

        public override void Entry(IModHelper helper)
        {
            this._config = this.Helper.ReadConfig<ModConfig>();
            PetDataService Data = new(ModManifest, Monitor);
            Multiplayer = new MultiplayerService(this);

            _petRegister = new PetRegister(Monitor, helper, () => _config, Data, Multiplayer);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.World.NpcListChanged += this.OnNpcListChanged;
            helper.Events.Display.Rendered += this.OnRendered;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            helper.Events.Multiplayer.ModMessageReceived += Multiplayer.OnModMessageReceived;
            helper.Events.Multiplayer.PeerConnected += Multiplayer.OnPeerConnected;
            Multiplayer.ToggleFollowRequested += _petRegister.HandleToggleFollowRequest;
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

            foreach (PetManager manager in _petRegister.AllManagers)
            {
                manager.StopFollowing();
                manager.OnDayStarted(sender, e);
            }

            if (_config.FillUpTeamOnDayStarted)
            {
                foreach (PetManager manager in _petRegister.AllManagers.Take(_config.MaxNumberFollowers))
                {
                    _petRegister.addToFollow(manager.pet, Game1.MasterPlayer, showFeedback: false);
                }  
                // fill up client as well?
            }

            // TODO: to be tested
            if (_config.WarpAllPetsBackToFarmHouseOnDayStarted)
            {
                _petRegister.ApplyToAllPets(pet =>
                {
                    Game1.warpCharacter(pet, "FarmHouse", Game1.MasterPlayer.TilePoint);
                });
            }

        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            _petRegister.ApplyToAllFollowingManagers(manager => manager.OnWarped(sender, e)); 

        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            _petRegister.ApplyToAllFollowingManagers(manager => manager.OnUpdateTicked(sender, e));
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
                // ? TODO update list 
                if (manager is not null)
                    Monitor.VerboseLog($"{manager.pet.name} is leaving us forever!!!");
            }
        }

        private void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            _petRegister.ApplyToAllFollowingManagers(manager => manager.OnRendered(sender, e));
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

            string? petId = _petRegister.FindAtTile(Game1.currentLocation, tileRect);

            if (petId is null)
            {
                Monitor.VerboseLog($"No pet found at {tileLocation}");
                return;
            }

            if (Context.IsMainPlayer)
                _petRegister.HandleToggleFollowRequest(Game1.player.UniqueMultiplayerID, petId);
            else
                Multiplayer.SendToggleFollowRequest(petId);     

        }

    }
}
