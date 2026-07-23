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
        public List<Pet> Pets = new();
        public PetRegister(IMonitor monitor)
        {
            Monitor = monitor;
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
