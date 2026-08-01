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

        private readonly PetMove _petMove;
        private readonly CombatService _combatService;
        private readonly PetRenderer _petRenderer;

        public PetManager(IMonitor monitor, Func<ModConfig> getConfig, IModHelper helper, Pet pet, PetDataService data)
        {
            Monitor = monitor;
            GetConfig = getConfig;
            Helper = helper;
            this.pet = pet;
            this.PetState = new PetState(pet, GetConfig, Monitor, data);
            PetState.initialize();

            _petMove = new PetMove(monitor, getConfig, pet, PetState);
            _petRenderer = new PetRenderer(Monitor, GetConfig, pet, PetState);
            _combatService = new CombatService(Monitor, GetConfig, Helper, pet, PetState);
            Data = data;
        }


        /* initialize services on day started
         * find pet and set it to pet move */
        public void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (pet == null) return;
            Monitor.Log($"Bringing {pet.Name} Today.", LogLevel.Info);

        }
        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (pet == null) return;

            // paused time for singer player & menu on
            if (!Game1.IsMultiplayer && Game1.activeClickableMenu != null)
            {
                return;
            }

            PetState.OnUpdateTicked(sender, e);

            if (GetConfig().EnablePetFollowing)
            {
                // use player for now, to be adapted for mult-p
                _petMove.OnUpdateTicked(sender, e, Game1.player);
            }

            if (GetConfig().EnableCombat)
            {
                _combatService.OnUpdateTicked(sender, e);
            }
        }

        public void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (pet == null) return;

            if (GetConfig().EnablePetFollowing)
            {
                _petMove.OnOwnerWarped(e.Player); // for now
            }

        }

        public void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (pet == null) return;
            _petRenderer.OnRendered(sender, e);

        }
    }
}
