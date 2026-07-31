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
        private Func<ModConfig> GetConfig;

        // all pets & corresponding managers
        public List<Pet> Pets = new();
        public List<PetManager> Managers = new();
        public PetRegister(IMonitor monitor, IModHelper helper, Func<ModConfig> getConfig)
        {
            Monitor = monitor;
            Helper = helper;
            GetConfig = getConfig;
        }

        public void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            Pets = getAllPetsInAllLocations();
            foreach (Pet pet in Pets)
            {
                var petManager = new PetManager(Monitor, GetConfig, this.Helper, pet);
                Managers.Add(petManager);
            }
        }

        public PetManager getManager(Pet pet)
        {
            return Managers.FirstOrDefault((manager) => pet.petId == manager.pet.petId) ?? 
                throw new InvalidOperationException($"Expected a PetManager for '{pet.name}' id {pet.petId}, but none was found.");
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

        public bool HasPets()
        {
            return Pets.Count > 0;
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
            foreach (var pet in Pets)
            {
                action(pet);
            }
        }
    }
}
