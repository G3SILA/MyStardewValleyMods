
namespace CombatPets
{
    public sealed class ModConfig
    {
        public bool EnablePetFollowing { get; set; } = true; 
        public int FollowDistance { get; set; } = 1; // tiles away the pet should follow
    }
}
