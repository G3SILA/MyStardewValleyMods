using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Pathfinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace CombatPets
{
    internal class PetFollowManager
    {
        private readonly IMonitor Monitor;
        private readonly ModConfig Config;

        private PetRegister _petRegister;
        private PetMove _petMove;

        // for now, just one pet
        private Pet? _pet;
        
        public PetFollowManager(IMonitor monitor, ModConfig config)
        {
            Monitor = monitor;
            Config = config;
            _petRegister = new PetRegister(monitor);
            _petMove = new PetMove(monitor, Config);
        }


        /* initialize services on day started
         * find pet and set it to pet move */
        public void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            _pet = _petRegister.getFirstPet();
            _petMove.pet = _pet;
            Monitor.Log($"Bringing {_pet.Name} Today.", LogLevel.Info);
            
        }
        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (_pet == null) return;

            // paused time for singer player & menu on
            if (!Game1.IsMultiplayer && Game1.activeClickableMenu != null)
            {
                return;
            }

            _petMove.OnUpdateTicked(sender, e);
        }

        public void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
        {
            _petRegister.OnNpcListChanged(sender, e);

            // is my pet still present? 
        }

        public void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (_pet == null) return;
            _petMove.OnWarped(sender, e);

        }
        
    }
}
