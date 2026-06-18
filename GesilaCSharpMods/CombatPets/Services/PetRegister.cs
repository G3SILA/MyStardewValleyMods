using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewValley.Characters;
using static StardewValley.Utility;
using StardewModdingAPI.Events;

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

        public void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
        {
            UpdatePetList();
        }
        private void UpdatePetList()
        {
            if(!Context.IsWorldReady)
            {
                return;
            }
            Pets = getAllPets();
            this.Monitor.Log($"Found {Pets.Count} pets.", LogLevel.Debug);
        }

        public bool HasPets()
        {
            return Pets.Count > 0;
        }

        // only one for now, can be modify in the future: 
        // highest friendship, multiple pets, etc.
        public Pet getFirstPet()
        {
            UpdatePetList();
            if (!HasPets()) 
            {
                return null;
            }
            return Pets[0];
        }

    }
}
