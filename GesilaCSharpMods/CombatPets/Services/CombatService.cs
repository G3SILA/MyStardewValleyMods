using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Monsters;


namespace CombatPets
{
    internal class CombatService
    {
        private Pet pet;
        private static IMonitor Monitor;
        private static Func<ModConfig>? GetConfig;

        private int attackCoolDown = 0;

        public CombatService(IMonitor monitor, Func<ModConfig>? getConfig, Pet pet)
        {
            Monitor = monitor;
            GetConfig = getConfig;
            this.pet = pet;
        }

        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(30)) return; // only check every 30 ticks (0.5 seconds)

            if (pet == null) return;

            Farmer player = Game1.player;
            GameLocation location = pet.currentLocation;
            if (location is not MineShaft || player.currentLocation != location)
            {
                return;
            }

            if (attackCoolDown > 0)
            {
                attackCoolDown--;
                return;
            }


            bool damaged = attackMonster();
            if (damaged)
            {
                attackCoolDown = 1; 
            } else
            {
                attackCoolDown = 0;
            }
            
        }

        public bool attackMonster()
        {
            GameLocation location = pet.currentLocation;
            Farmer player = Game1.player;
            if (location == null || location != player.currentLocation) return false;



            Rectangle damageArea = getAttackArea();
            int baseDamage = getAttackDamage();

            // inherit lucky etc. buff from player
            bool damaged = location.damageMonster(damageArea, baseDamage, (int)(baseDamage * 1.2f), false, player);

            /*
             TODO: area damage, size / direction
                   manual knock back? (avoid direction issue? play first before try.)
                   inherit lucky etc. buff from player?
             */
            Monitor.Log($"Pet position: {pet.position.Value}, pet attack: {damageArea}", LogLevel.Debug);
            Monitor.Log($"Attack status {damaged}", LogLevel.Debug);
            return damaged;

        }

        private Rectangle getAttackArea()
        {
            Vector2 nextPoint = Utilities.GetPositionOfDirection(pet.Position, pet.getFacingDirection());
            Rectangle damageArea = new Rectangle((int)nextPoint.X, (int)nextPoint.Y, 100, 100);
            return damageArea;
        }

        // attack damage based on friendship level and config 
        // base: 2-8
        private int getAttackDamage()
        {
            int friendship = pet.friendshipTowardFarmer.Value;

            float strengthMagnification = getStrengthMagnification();

            int baseDamage = (int) ((friendship / 150 + 2) * strengthMagnification);

            return baseDamage;
        }

        private float getStrengthMagnification()
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

    }
}
