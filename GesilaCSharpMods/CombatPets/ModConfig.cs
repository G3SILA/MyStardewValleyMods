

using StardewModdingAPI;

namespace CombatPets
{
    public sealed class ModConfig
    {
        
        public bool EnablePetFollowing { get; set; } = true;

        public SButton TogglePetFollowingKeybind { get; set; } = SButton.MouseRight;
        public int MaxNumberFollowers { get; set; } = 3;
        public int FollowDistance { get; set; } = 3; // tiles away the pet should follow
        public int AddedFollowSpeed { get; set; } = 3; 
        public bool SoundOnJumpPet { get; set; } = true;

        // 60 ticks = 1 second, time in ticks
        public int TimeToWarpWhenNoPathFound { get; set; } = 30;


        ////////////////////////////////// Combat //////////////////////////////////////
        public bool EnableCombat { get; set; } = true;

        public PetStrength PetStrength { get; set; } = PetStrength.Normal;

        public ShowHealthBar ShowHealthBar { get; set; } = ShowHealthBar.InCombat;
    }

    public enum PetStrength
    {
        Helpful,
        Normal,
        Overpowered
    }
    public enum ShowHealthBar
    {
        Always,
        InCombat,
        Never
    }

}
