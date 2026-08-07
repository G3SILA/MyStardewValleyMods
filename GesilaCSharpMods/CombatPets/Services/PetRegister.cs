using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.Logging;


namespace CombatPets
{
    /// <summary>
    /// Handle pet registration. This class is responsible for tracking all pets in the game and handling interactions such as following players.
    /// </summary>
    internal sealed class PetRegister
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        private readonly Func<ModConfig> GetConfig;
        public PetDataService Data { get; }

        private readonly MultiplayerService Multiplayer;

        private readonly Dictionary<string, PetManager> Managers = new();
        public IEnumerable<PetManager> AllManagers => Managers.Values;
        public int following => AllManagers.Count(manager => manager.IsFollowing);
        public PetRegister(IMonitor monitor, IModHelper helper, Func<ModConfig> getConfig, PetDataService data, MultiplayerService mult)
        {
            Monitor = monitor;
            Helper = helper;
            GetConfig = getConfig;
            Data = data;
            Multiplayer = mult;
        }

        public void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            Managers.Clear();
            Refresh();
        }

        public void Refresh()
        {
            if (!Context.IsWorldReady) return;

            Monitor.VerboseLog("Refreshing pet managers");

            Dictionary<string, Pet> foundPets = new();
            IEnumerable<GameLocation> activeLocations = Helper.Multiplayer.GetActiveLocations();
            IEnumerable<GameLocation> locations = Context.IsMainPlayer? Game1.locations.Concat(activeLocations).Distinct() : activeLocations;

            foreach (GameLocation location in locations)
            {
                foreach (NPC npc in location.characters)
                {
                    if (npc is not Pet pet)
                        continue;

                    string? petId = GetPetId(pet);

                    if (petId is not null)
                        foundPets[petId] = pet;
                }
            }

            foreach ((string petId, Pet pet) in foundPets)
            {
                if (Managers.TryGetValue(petId, out PetManager? existing)
                    && ReferenceEquals(existing.pet, pet))
                {
                    continue;
                }
                // update if reference is different, or create new manager if not found
                // update reference for later render & follow checks of clients
                var petManager = new PetManager(Monitor, GetConfig, this.Helper, pet, Data, petId, Multiplayer);
                Managers[petId] = petManager;
            }

            foreach (string missingId in Managers.Keys.Except(foundPets.Keys).ToArray()) 
            {
                // only remove if pet is no longer on its current location, that is removed from the world.
                // otherwise, it may be a multiplayer client sync issue in the mineshaft
                if (!IsPetInItsCurrentLocation(Managers[missingId]))
                {
                    Managers.Remove(missingId);
                    Monitor.VerboseLog($"Removed pet manager for missing pet {missingId}");
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

        [Obsolete("Use refresh instead")]
        public Pet? IsPetRemoved(object? sender, NpcListChangedEventArgs e)
        {
            // TODO: must be updated for multiplayer, as the pet may not be at the same position as main player
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

        /// <summary>
        /// if pet is in its current location. Use to check if it is removed from the world.
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        private bool IsPetInItsCurrentLocation(PetManager manager)
        {
            Pet pet = manager.pet;
            GameLocation? location = pet.currentLocation;

            return location is not null &&
                   location.characters.Contains(pet);
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

        public void ApplyToAllFollowingManagers(Action<PetManager> action)
        {
            foreach (var manager in AllManagers)
            {
                if (manager.IsFollowing)
                {
                    action(manager);
                }
            }
        }

        public string? GetPetId(Pet pet)
        {
            Guid id = pet.petId.Value;
            return id == Guid.Empty ? null : id.ToString("N");
        }

        /// <summary>
        /// return petID if a pet is found at the given tile area, otherwise return null
        /// return id since client may not have the same pet object as the server
        /// </summary>
        /// <param name="location"></param>
        /// <param name="tileArea"></param>
        /// <returns></returns>
        public string? FindAtTile(GameLocation location, Rectangle tileArea)
        {
            Pet? clickPet = Game1.currentLocation.characters.OfType<Pet>().FirstOrDefault(pet =>
                pet.GetBoundingBox().Intersects(tileArea));

            return clickPet is not null ? GetPetId(clickPet) : null;
        }



        public void addToFollow(Pet pet, Farmer owner, bool showFeedback = true)
        {
            var manager = getManager(pet);

            manager.AssignOwner(owner.UniqueMultiplayerID);
            if (showFeedback)
            {
                pet.playContentSound();
                pet.doEmote(56);
            }

        }

        public void removeFromFollow(Pet pet)
        {
            var manager = getManager(pet);
            manager.StopFollowing();
        }


        public void HandleToggleFollowRequest(long requesterId, string petId)
        {
            if (!Context.IsMainPlayer) return;

            Farmer? requester = Game1.getOnlineFarmers().FirstOrDefault(farmer => farmer.UniqueMultiplayerID == requesterId);
            PetManager? manager = getManager(petId);


            ToggleFollowResultMessage result;
            if (requester is null || manager is null)
            {
                result = ToggleFollowResultMessage.Failure(petId, manager?.pet.Name ?? "", "not-found");
            }
            else if (requester.currentLocation is MineShaft)
            {
                result = ToggleFollowResultMessage.Failure(petId, manager.pet.Name, "disabled-in-mines");
            }
            else if (manager.IsFollowing && manager.OwnerId != requesterId)
            {
                result = ToggleFollowResultMessage.Failure(petId, manager.pet.Name, "owned");
            }
            else if (manager.IsFollowing)
            {
                removeFromFollow(manager.pet);
                result = ToggleFollowResultMessage.SuccessToggle(manager, isFollowing: false);
            }
            else if (following >= GetConfig().MaxNumberFollowers)
            {
                result = ToggleFollowResultMessage.Failure(petId, manager.pet.Name, "capacity");
            }
            else
            {
                addToFollow(manager.pet, requester);
                result = ToggleFollowResultMessage.SuccessToggle(manager, isFollowing: true);
            }

            if (requesterId == Game1.player.UniqueMultiplayerID)
                Multiplayer.ShowToggleFollowResult(result);
            else
                Multiplayer.SendToggleFollowResult(requesterId, result);
        }
    }
}
