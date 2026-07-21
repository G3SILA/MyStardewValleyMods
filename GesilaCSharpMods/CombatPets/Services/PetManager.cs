using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;


namespace CombatPets
{
    internal class PetManager
    {
        private static IMonitor Monitor;
        private static IModHelper Helper;
        private static Func<ModConfig> GetConfig;

        public Pet pet;
        public PetState PetState;
        private PetMove _petMove;
        private CombatService _combatService;
        private PetRenderer _petRenderer;

        public PetManager(IMonitor monitor, Func<ModConfig> getConfig, IModHelper helper, Pet pet)
        {
            Monitor = monitor;
            GetConfig = getConfig;
            Helper = helper;
            this.pet = pet;
            _petMove = new PetMove(monitor, getConfig);

        }


        /* initialize services on day started
         * find pet and set it to pet move */
        public void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (pet == null) return;
            _petMove.pet = pet;
            Monitor.Log($"Bringing {pet.Name} Today.", LogLevel.Info);

            this.PetState = new PetState(pet, GetConfig, Monitor);
            PetState.initialize();
            _petMove.PetState = PetState;
            _petRenderer = new PetRenderer(Monitor, GetConfig, pet, PetState);
            _combatService = new CombatService(Monitor, GetConfig, Helper, pet, PetState);

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
                _petMove.OnUpdateTicked(sender, e);
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
                _petMove.OnWarped(sender, e);
            }

        }

        public void OnRendered(object? sender, RenderedEventArgs e)
        {
            if (pet == null) return;
            _petRenderer.OnRendered(sender, e);

        }
    }
}
