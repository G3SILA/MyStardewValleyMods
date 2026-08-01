using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace CombatPets
{
    /// <summary>
    /// Store shared data for multiplayer pets, to be synchronized between players.
    /// </summary>
    internal sealed class PetDataService
    {
        private readonly IMonitor Monitor;
        private readonly string Prefix;

        private string FollowingKey => $"{Prefix}/Following";
        private string OwnerIdKey => $"{Prefix}/OwnerId";

        // for rendering 
        private string MaxHealthKey => $"{Prefix}/MaxHealth";
        private string HealthKey => $"{Prefix}/Health";
        private string StateKey => $"{Prefix}/State";

        public PetDataService(IManifest manifest, IMonitor monitor)
        {
            Prefix = manifest.UniqueID;
            Monitor = monitor;
        }

        public bool IsFollowing(Pet pet)
        {
            return GetBool(pet, FollowingKey, false);
        }

        public long? GetOwnerId(Pet pet)
        {
            if (!pet.modData.TryGetValue(OwnerIdKey, out string? raw)) return null;

            return long.TryParse(raw, out long ownerId)? ownerId : null;
        }

        /// <summary>
        /// null ownerId would remove from following and remove ownerId from modData
        /// </summary>
        /// <param name="pet"></param>
        /// <param name="ownerId"></param>
        public void SetFollowingOwner(Pet pet, long? ownerId)
        {
            if (!Game1.IsMasterGame) { return; }

            if (ownerId.HasValue)
            {
                pet.modData[FollowingKey] = bool.TrueString;
                pet.modData[OwnerIdKey] = ownerId.Value.ToString();
            }
            else
            {
                pet.modData[FollowingKey] = bool.FalseString;
                pet.modData.Remove(OwnerIdKey);
            }
        }

        public bool HasMaxHealth(Pet pet) => pet.modData.ContainsKey(MaxHealthKey);
        public bool HasHealth(Pet pet) => pet.modData.ContainsKey(HealthKey);
        public bool HasState(Pet pet) => pet.modData.ContainsKey(StateKey);

        public int GetMaxHealth(Pet pet, int defaultValue = 1)
        {
            return GetInt(pet, MaxHealthKey, defaultValue);
        }

        public void SetMaxHealth(Pet pet, int value)
        {
            if (!Game1.IsMasterGame) { return; }
            pet.modData[MaxHealthKey] = Math.Max(1, value).ToString();
        }

        public int GetHealth(Pet pet, int defaultValue = 1)
        {
            return GetInt(pet, HealthKey, defaultValue);
        }

        public void SetHealth(Pet pet, int value)
        {
            if (!Game1.IsMasterGame) { return; }
            int maxHealth = GetMaxHealth(pet, Math.Max(1, value));
            pet.modData[HealthKey] = Math.Clamp(value, 0, maxHealth).ToString();
        }

        public PetStateEnum GetState(Pet pet)
        {
            if (pet.modData.TryGetValue(StateKey, out string? raw)
                && Enum.TryParse(raw, ignoreCase: true, out PetStateEnum state))
            {
                return state;
            }
            Monitor.VerboseLog("Cannot parse state");
            return PetStateEnum.Idle;
        }

        public void SetState(Pet pet, PetStateEnum state)
        {
            if (!Game1.IsMasterGame) { return; }
            pet.modData[StateKey] = state.ToString();
        }

        private static int GetInt(Pet pet, string key, int defaultValue)
        {
            return pet.modData.TryGetValue(key, out string? raw) && int.TryParse(raw, out int value)
                ? value
                : defaultValue;
        }

        private static bool GetBool(Pet pet, string key, bool defaultValue)
        {
            return pet.modData.TryGetValue(key, out string? raw) && bool.TryParse(raw, out bool value)
                ? value
                : defaultValue;
        }
    }

}
