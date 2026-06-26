
namespace CombatPets
{
    public sealed class ModConfig
    {
        
        public bool EnablePetFollowing { get; set; } = true; 
        public int FollowDistance { get; set; } = 1; // tiles away the pet should follow
        public int AddedFollowSpeed { get; set; } = 3; 
        public bool SoundOnJumpPet { get; set; } = true;

        // larger collision would get a better path find, reduce stuck but increase no path found.
        public bool LargerCollisionEnabled { get; set; } = true;

        // 60 ticks = 1 second, time in ticks; this property is more important with LargerCollisionEnabled
        public int TimeToWarpWhenNoPathFound { get; set; } = 15;
    }


    
}
