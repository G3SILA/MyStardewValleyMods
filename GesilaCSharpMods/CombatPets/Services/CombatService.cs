using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects.Trinkets;
using StardewValley.Tools;


namespace CombatPets
{
    internal class CombatService
    {
        private Pet pet;
        private static IMonitor Monitor;
        private static Func<ModConfig>? GetConfig;

        private PetState PetState;

        private static AnimationManager _animationManager;

        private int attackCoolDown = 0;

        public CombatService(IMonitor monitor, Func<ModConfig>? getConfig, IModHelper helper, Pet pet, PetState state)
        {
            Monitor = monitor;
            _animationManager = new AnimationManager(helper);
            GetConfig = getConfig;
            this.pet = pet;
            PetState = state;
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

            checkDamageFromMonster(location);

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
                PlayAttackEffects();
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

        private void PlayAttackEffects()
        {
            pet.shakeTimer = 250;
        }

        public void takeDamage(int damage, Monster damager)
        {
            Farmer player = Game1.player;
            if (Game1.eventUp || player.FarmerSprite.isPassingOut() || (player.isInBed.Value && Game1.activeClickableMenu != null && Game1.activeClickableMenu is ReadyCheckDialog))
            {
                return;
            }
            if (PetState.IsInvincible()) { return; }
            if (damager == null || damager.isInvincible()) { return; }
            
            // pet do inherit buff & rings from player
            bool monsterDamageCapable = (damager == null || !damager.isInvincible()) && (damager == null || (!(damager is GreenSlime) && !(damager is BigSlime)) || !player.isWearingRing("520"));

            if (!monsterDamageCapable)
            {
                return;
            }

            damage += Game1.random.Next(Math.Min(-1, -damage / 8), Math.Max(1, damage / 8));

            int defense = player.buffs.Defense;
            if (player.stats.Get("Book_Defense") != 0)
            {
                defense++;
            }
            if (defense >= damage * 0.5f)
            {
                defense -= (int)(defense * Game1.random.Next(3) / 10f);
            }

            // thron ring effect: damage to monster when player is wearing thron ring
            if (damager != null && player.isWearingRing("839"))
            {
                Rectangle monsterBox = damager.GetBoundingBox();
                Vector2 trajectory = Utility.getAwayFromPlayerTrajectory(monsterBox, player);
                trajectory /= 2f;
                int damageToMonster = damage;
                int farmerDamage = Math.Max(1, damage - defense);
                if (farmerDamage < 10)
                {
                    damageToMonster = (int)Math.Ceiling((damageToMonster + farmerDamage) / 2.0);
                }
                damager.takeDamage(damageToMonster, (int)trajectory.X, (int)trajectory.Y, isBomb: false, 1.0, player);
                damager.currentLocation.debris.Add(new Debris(damageToMonster, new Vector2(monsterBox.Center.X + 16, monsterBox.Center.Y), new Color(255, 130, 0), 1f, damager));
            }

            // low health chance to trigger yoba ring effect
            if (player.isWearingRing("524") && !player.hasBuff("21") && Game1.random.NextDouble() < (0.9 - (double)(PetState.Health / 100f)) / (double)(3 - player.LuckLevel / 10) + ((PetState.Health <= 15) ? 0.2 : 0.0))
            {
                pet.playNearbySoundAll("yoba");
                PetState.SetInvincible(300);
                return;
            }

            damage = Math.Max(1, damage - defense);

            // desert festival
            if (Utility.GetDayOfPassiveFestival("DesertFestival") > 0 && player.currentLocation is MineShaft && Game1.mine.getMineArea() == 121)
            {
                float adjustment = 1f;
                if (player.team.calicoStatueEffects.TryGetValue(8, out var sharpTeethAmount))
                {
                    adjustment += (float)sharpTeethAmount * 0.25f;
                }
                if (player.team.calicoStatueEffects.TryGetValue(14, out var toothFileAmount))
                {
                    adjustment -= (float)toothFileAmount * 0.25f;
                }
                damage = Math.Max(1, (int)((float)damage * adjustment));
            }

            // damaged
            PetState.Health = Math.Max(0, PetState.Health - damage);
            
            PetState.SetInvincible(60);

            Point standingPixel = pet.StandingPixel;
            pet.currentLocation.debris.Add(new Debris(damage, new Vector2(standingPixel.X + 8, standingPixel.Y), Color.Yellow, 1f, pet));
            pet.playNearbySoundAll("ow");

            Monitor.Log($"Damage: {damage}, Health: {PetState.Health}", LogLevel.Debug);

        }

        private void checkDamageFromMonster(GameLocation location)
        {
            if (Game1.eventUp)
            {
                return;
            }
            for (int i = location.characters.Count - 1; i >= 0; i--)
            {
                if (i < location.characters.Count && location.characters[i] is Monster monster && Utilities.IsCharacterColliding(monster, pet))
                {
                    monster.currentLocation = location;
                    monster.collisionWithFarmerBehavior();
                    if (monster.DamageToFarmer > 0)
                    {
                        takeDamage(monster.DamageToFarmer, monster);
                    }
                }
            }
        }

    }
}
