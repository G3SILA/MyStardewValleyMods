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

        private static AnimationManager _animationManager;

        private int attackCoolDown = 0;

        public CombatService(IMonitor monitor, Func<ModConfig>? getConfig, IModHelper helper, Pet pet)
        {
            Monitor = monitor;
            _animationManager = new AnimationManager(helper);
            GetConfig = getConfig;
            this.pet = pet;
        }

        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(15)) return; // only check every 15 ticks (0.25 seconds)

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
                attackCoolDown = 2; 
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


            if (damaged)
            {
                _animationManager.DrawAttack(location, damageArea, pet.FacingDirection == 1 || pet.FacingDirection == 3);
            }
            Monitor.Log($"Attack status {damaged}", LogLevel.Trace);
            return damaged;

        }

        private Rectangle getAttackArea()
        {
            int direction = pet.getFacingDirection();
            Vector2 nextPoint = Utilities.GetPositionOfDirection(pet.getStandingPosition(), direction);
            Rectangle damageArea = new Rectangle((int)nextPoint.X - 50, (int)nextPoint.Y - 50, 100, 100);
            if (direction == 1 || direction == 3)
            {
                damageArea = new Rectangle((int)nextPoint.X - 50, (int)nextPoint.Y - 70, 100, 100);
            } 
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
