
namespace CombatPets
{
    public sealed class ModConfig
    {
        
        public bool EnablePetFollowing { get; set; } = true; 
        public int FollowDistance { get; set; } = 1; // tiles away the pet should follow

        // larger collision would get a better path find, reduce stuck but increase no path found.
        public bool LargerCollisionEnabled { get; set; } = false;

        // not implemented yet.
        public int TimeToWarpWhenNoPathFound { get; set; } = 15;
    }


    
}
