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
        private static IMonitor Monitor;
        private static IModHelper Helper;
        private static Func<ModConfig>? GetConfig;

        private PetRegister _petRegister;
        private PetMove _petMove;
        private CombatService _combatService;

        // for now, just one pet
        private Pet? _pet;
        
        public PetFollowManager(IMonitor monitor, Func<ModConfig> getConfig, IModHelper helper)
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
            _pet = _petRegister.getFirstPet();
            _petMove.pet = _pet;
            Monitor.Log($"Bringing {_pet.Name} Today.", LogLevel.Info);
            _combatService = new CombatService(Monitor, GetConfig, Helper, _pet);

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
            _combatService.OnUpdateTicked(sender, e);
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
