using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;


namespace CombatPets
{
    internal class PetManager
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        private readonly Func<ModConfig> GetConfig;
        private readonly PetDataService Data;

        public Pet pet;
        public PetState PetState;

        public string PetId { get; }
        public bool IsFollowing => Data.IsFollowing(pet);
        public long? OwnerId => Data.GetOwnerId(pet);

        private readonly PetMove _petMove;
        private readonly CombatService _combatService;
        private readonly PetRenderer _petRenderer;

        public PetManager(IMonitor monitor, Func<ModConfig> getConfig, IModHelper helper, Pet pet, PetDataService data, string id, MultiplayerService multiplayer)
        {
            Monitor = monitor;
            GetConfig = getConfig;
            Helper = helper;
            this.pet = pet;
            this.PetState = new PetState(pet, GetConfig, Monitor, data);
            PetState.initialize();
            PetId = id;

            _petMove = new PetMove(monitor, getConfig, pet, PetState);
            _petRenderer = new PetRenderer(Monitor, GetConfig, pet, PetState);
            _combatService = new CombatService(Monitor, GetConfig, Helper, pet, PetState, multiplayer);
            Data = data;
        }

        public void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!Context.IsMainPlayer) return;
            PetState.initialize();
        }
        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            // paused time for singer player & menu on
            if (!Game1.IsMultiplayer && Game1.activeClickableMenu != null)
            {
                return;
            }

            PetState.OnUpdateTicked(sender, e);

            // following
            if (!Context.IsMainPlayer || !IsFollowing) return;

            Farmer? owner = GetOwner();
            if (owner is null || owner.currentLocation is null) return;

            ModConfig config = GetConfig();

            if (!config.EnableCombat
                && PetState.State is not PetStateEnum.Defeated)
            {
                PetState.SetState(PetStateEnum.Following);
            }

            if (GetConfig().EnablePetFollowing)
            {
                _petMove.OnUpdateTicked(sender, e, owner);
            }

            if (GetConfig().EnableCombat)
            {
                _combatService.OnUpdateTicked(sender, e, owner);
            }
        }

        public void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (GetConfig().EnablePetFollowing && IsFollowing)
            {
                _petMove.OnOwnerWarped(e.Player); // for now
            }

        }

        public void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (IsFollowing)
                _petRenderer.OnRendered(sender, e);

        }

        public Farmer? GetOwner()
        {
            if (OwnerId is null) return null;
            return Game1.getOnlineFarmers().FirstOrDefault(farmer => farmer.UniqueMultiplayerID == OwnerId.Value);
        }

        public void AssignOwner(long ownerId)
        {
            if (!Context.IsMainPlayer) return;
            Data.SetFollowingOwner(pet, ownerId);
            PetState.SetState(PetStateEnum.Following);
        }

        public void StopFollowing()
        {
            if (!Context.IsMainPlayer) return;
            Data.SetFollowingOwner(pet, null);
            PetState.SetState(PetStateEnum.Idle);
        }
    }
}
