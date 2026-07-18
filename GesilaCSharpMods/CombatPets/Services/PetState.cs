using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace CombatPets
{
    internal class PetState
    {
        public Pet pet; 
        private static Func<ModConfig>? GetConfig;
        private static IMonitor Monitor;

        public PetState(Pet pet, Func<ModConfig>? getConfig, IMonitor monitor)
        {
            this.pet = pet;
            GetConfig = getConfig;
            Monitor = monitor;
        }

        public int MaxHealth;
        public int Health;
        public int InvincibleCountDown { get; private set; } = 0;
        public int AttackedCountDown { get; private set; } = 0;
        public PetStateEnum State { get; set; } = PetStateEnum.Idle;

        public void Attacked()
        {
            SetInvincible(60);
            AttackedCountDown = 120;
        }
        public bool IsAttacked()
        {
            return AttackedCountDown > 0;
        }

        public void initialize()
        {
            Farmer player = Game1.player;
            MaxHealth = (int)((pet.friendshipTowardFarmer.Value / 10 + 50) * getStrengthMagnification());
            Health = MaxHealth;
            State = PetStateEnum.Idle;
        }

        public float getStrengthMagnification()
        {
            PetStrength strength = GetConfig!().PetStrength;
            if (strength == PetStrength.Helpful)
            {
                return 0.75f;
            }
            else if (strength == PetStrength.Normal)
            {
                return 1.0f;
            }
            else if (strength == PetStrength.Overpowered)
            {
                return 1.5f;
            }
            else
            {
                Monitor.Log($"Unknown pet strength: {strength}", LogLevel.Warn);
                return 1.0f;
            }
        }

        public bool IsInvincible()
        {
            return InvincibleCountDown > 0;
        }
        public void SetInvincible(int ticks)
        {
            if (InvincibleCountDown > ticks) return;
            InvincibleCountDown = ticks;
        }

        public bool IsAlive()
        {
            return Health > 0;
        }

        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (InvincibleCountDown > 0)
            {
                InvincibleCountDown--;
            }
            if (AttackedCountDown > 0)
            {
                AttackedCountDown--;
            }

            // restore health over time
            if (e.IsMultipleOf(60) && State != PetStateEnum.Defeated)
            {
                if (Health > 0 && Health < MaxHealth)
                {
                    ++Health;
                }

            }
        }

    }

    

    public enum PetStateEnum
    {
        Idle,
        Following,
        Combat,    // in mine & enable combat
        Attacking, // attack monster animation
        Defeated
    }
}
