using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;

/*
    Find all pets in the world and store them in a list

    TODO: handle update of add/remove pet
*/

namespace CombatPets
{
    internal sealed class PetRegister
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        private readonly Func<ModConfig> GetConfig;
        public PetDataService Data { get; }

        private readonly Dictionary<string, PetManager> Managers = new();
        public IEnumerable<PetManager> AllManagers => Managers.Values;
        public PetRegister(IMonitor monitor, IModHelper helper, Func<ModConfig> getConfig, PetDataService data)
        {
            Monitor = monitor;
            Helper = helper;
            GetConfig = getConfig;
            Data = data;
        }

        public void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            List<Pet> Pets = getAllPetsInAllLocations();
            foreach (Pet pet in Pets)
            {
                string id = GetPetId(pet);

                var petManager = new PetManager(Monitor, GetConfig, this.Helper, pet, Data);
                if (id is not null)
                {
                    Managers[id] = petManager;
                }
            }
        }

        public PetManager? getManager(string petId)
        {
            Managers.TryGetValue(petId, out PetManager? manager);
            return manager;
        }

        public PetManager? getManager(Pet pet)
        {
            string? petId = GetPetId(pet);
            return petId is null ? null : getManager(petId);
        }

        public Pet? IsPetRemoved(object? sender, NpcListChangedEventArgs e)
        {
            if (!e.IsCurrentLocation)
            {
                return null;
            }

            // a pet is removed from current location and not added to another -> removed from the world
            foreach (NPC npc in e.Removed)
            {
                if (npc is Pet pet)
                {
                    List<Pet> currPets = getAllPetsInAllLocations();
                    if (!currPets.Contains(pet))
                    {
                        return pet;
                    }
                }
            }
            return null;
        }

        public List<Pet> getAllPetsInAllLocations()
        {
            List<Pet> allPets = new();
            foreach (GameLocation location in Game1.locations)
            {
                foreach (NPC npc in location.characters)
                {
                    if (npc is Pet pet)
                    {
                        allPets.Add(pet);
                    }
                }
            }
            return allPets;
        }
        public void ApplyToAllPets(Action<Pet> action)
        {
            foreach (var manager in AllManagers)
            {
                action(manager.pet);
            }
        }

        public string? GetPetId(Pet pet)
        {
            Guid id = pet.petId.Value;
            return id == Guid.Empty ? null : id.ToString("N");
        }
    }
}
