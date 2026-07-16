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

        private PetRegister _petRegister;

        public Pet pet;
        public PetState PetState;
        private PetMove _petMove;
        private CombatService _combatService;
        
        public PetManager(IMonitor monitor, Func<ModConfig> getConfig, IModHelper helper)
        {
            Monitor = monitor;
            GetConfig = getConfig;
            Helper = helper;
            _petRegister = new PetRegister(monitor);
            _petMove = new PetMove(monitor, getConfig);

        }


        /* initialize services on day started
         * find pet and set it to pet move */
        public void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            pet = _petRegister.getFirstPet();
            _petMove.pet = pet;
            Monitor.Log($"Bringing {pet.Name} Today.", LogLevel.Info);

            this.PetState = new PetState(pet);
            PetState.initialize();
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

            if (GetConfig().EnableCombat) {
                _combatService.OnUpdateTicked(sender, e);
            }
        }

        public void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
        {
            _petRegister.OnNpcListChanged(sender, e);

            // is my pet still present? 
        }

        public void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (pet == null) return;

            if (GetConfig().EnablePetFollowing)
            {
                _petMove.OnWarped(sender, e);
            }
           
        }
        
    }
}
