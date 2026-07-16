using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace CombatPets
{
    internal class PetState
    {
        Pet pet; 
        public PetState(Pet pet)
        {
            this.pet = pet;
        }

        public void initialize()
        {
            Farmer player = Game1.player;
            MaxHealth = pet.friendshipTowardFarmer.Value / 10 + 50;
            Health = MaxHealth;
            State = PetStateEnum.Idle;
        }
        public int MaxHealth;
        public int Health;
        public PetStateEnum State { get; set; } = PetStateEnum.Idle;

        public bool IsInvincible()
        {
            return InvincibleCountDown > 0;
        }
        public void SetInvincible(int ticks)
        {
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
        }

        private int InvincibleCountDown = 0;

    }

    }

    public enum PetStateEnum
    {
        Idle,
        Following,
        Attacking,
        Defeated,
        Hurt
    }
}
