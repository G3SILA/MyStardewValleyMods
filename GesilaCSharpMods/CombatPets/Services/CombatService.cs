using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;


namespace CombatPets
{
    internal class CombatService
    {
        private readonly Pet pet;
        private readonly IMonitor Monitor;
        private readonly Func<ModConfig>? GetConfig;
        private readonly IModHelper Helper;
        private readonly MultiplayerService Multiplayer;

        private readonly PetState PetState;

        private int attackCoolDown = 0;

        public CombatService(IMonitor monitor, Func<ModConfig>? getConfig, IModHelper helper, Pet pet, PetState state, MultiplayerService multiplayer)
        {
            Monitor = monitor;
            Helper = helper;
            GetConfig = getConfig;
            this.pet = pet;
            PetState = state;
            Multiplayer = multiplayer;
        }

        // only called when EnableCombat is true
        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e, Farmer player)
        {
            // combat service handled by host only
            if (!Context.IsMainPlayer || pet == null || PetState.State == PetStateEnum.Defeated)
                return;

            GameLocation location = pet.currentLocation;
            if (location is not MineShaft || player.currentLocation != location)
            {
                return;
            }


            if (PetState.State != PetStateEnum.Attacking)
            {
                PetState.SetState(PetStateEnum.Combat);
            }

            if (e.IsOneSecond) Monitor.VerboseLog($"Pet: {pet.name}, State: {PetState.State}, Health: {PetState.Health}");

            checkDamageFromMonster(location, player);

            if (attackCoolDown > 0)
            {
                attackCoolDown--;
                return;
            }


            bool damaged = attackMonster(player);
            if (damaged)
            {
                attackCoolDown = 30; 
            } else
            {
                attackCoolDown = 0;
            }
            
        }

        public bool attackMonster(Farmer player)
        {
            GameLocation location = pet.currentLocation;
            if (location == null || location != player.currentLocation) return false;

            Rectangle damageArea = getAttackArea();
            int baseDamage = getAttackDamage();

            // inherit lucky etc. buff from player
            bool damaged = location.damageMonster(damageArea, baseDamage, (int)(baseDamage * 1.2f), false, player);

            if (damaged)
            {
                PlayAttackEffects();
                Multiplayer.BroadcastAttackEffect(location, damageArea, pet.FacingDirection is 1 or 3);

                PetState.SetState(PetStateEnum.Attacking);
                DelayedAction.functionAfterDelay(() => {
                    if (PetState.IsAlive()) PetState.SetState(PetStateEnum.Combat);
                    else PetState.SetState(PetStateEnum.Defeated);
                }, 300);
            }
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

            float strengthMagnification = PetState.getStrengthMagnification();

            int baseDamage = (int) ((friendship / 150 + 2) * strengthMagnification);

            return baseDamage;
        }

        private void PlayAttackEffects()
        {
            pet.shakeTimer = 250;
        }

        public void takeDamage(int damage, Monster damager, Farmer player)
        {
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
            PetState.SetHealth(Math.Max(0, PetState.Health - damage));

            bool defeated = !PetState.IsAlive();
            if (defeated) PetState.SetState(PetStateEnum.Defeated);

            Multiplayer.BroadcastPetHit(pet, damage, 60, 120, defeated);
            Monitor.VerboseLog($"Pet {pet.Name} took {damage} damage. Health: {PetState.Health}/{PetState.MaxHealth}.");

        }

        private void checkDamageFromMonster(GameLocation location, Farmer owner)
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
                        takeDamage(monster.DamageToFarmer, monster, owner);
                    }
                }
            }
        }

    }
}
