using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace CombatPets
{
    internal class PetState
    {
        private readonly Pet pet;
        private readonly PetDataService Data;
        private readonly Func<ModConfig>? GetConfig;
        private readonly IMonitor Monitor;

        public PetState(Pet pet, Func<ModConfig>? getConfig, IMonitor monitor, PetDataService data)
        {
            this.pet = pet;
            GetConfig = getConfig;
            Monitor = monitor;
            Data = data;
        }

        public int MaxHealth => Data.GetMaxHealth(pet, 1);
        public int Health => Data.GetHealth(pet, MaxHealth);
        public int InvincibleCountDown { get; private set; } = 0;
        public int AttackedCountDown { get; private set; } = 0;
        public PetStateEnum State => Data.GetState(pet);

        public void SetState(PetStateEnum state)
        {
            if (!Context.IsMainPlayer || State == state) return;

            Data.SetState(pet, state);
        }

        public void SetHealth(int value)
        {
            if (!Context.IsMainPlayer) return;

            Data.SetHealth(pet, value);
        }

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
            if (!Context.IsMainPlayer) return;

            Data.SetMaxHealth( pet, (int)((pet.friendshipTowardFarmer.Value / 10 + 50) * getStrengthMagnification()));
            SetHealth(MaxHealth);
            SetState(PetStateEnum.Idle);
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

        /// <summary>
        /// set to ticks if current invincible countdown is less than ticks, otherwise do nothing
        /// </summary>
        /// <param name="ticks"></param>
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

            // below is only for host
            if (!Game1.IsMasterGame) return;

            // restore health over time
            if (e.IsMultipleOf(60) && State != PetStateEnum.Defeated)
            {
                if (Health > 0 && Health < MaxHealth)
                {
                    SetHealth(Health + 1);
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
