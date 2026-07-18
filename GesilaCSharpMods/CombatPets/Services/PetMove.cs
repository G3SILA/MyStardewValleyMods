using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Pathfinding;


namespace CombatPets
{
    internal class PetMove
    {
        public Pet pet;
        public PetState PetState;
        private static IMonitor Monitor;
        private static Func<ModConfig>? GetConfig;

        // path finding set
        private Point? _lastDestination;
        private int _repathCooldown = 0;
        private int _noPathFoundWait = 0;

        private Point _lastTile;
        private Vector2 _playerLastPosition;
        private int stuckCounter = 0;
        const int RePathCoolDown = 15;

        // attack mode set
        private bool attackMode = false;
        const int DetactMonsterDistance = 3;
        const int PlayerTooFarDistance = 4;
        //

        public PetMove(IMonitor monitor, Func<ModConfig>? getConfig)
        {
            Monitor = monitor;
            GetConfig = getConfig;
            PetPathFinding.initialize(GetConfig);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DebugLog(string message, LogLevel level)
        {
            Monitor.Log(message, level);
        }

        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (pet == null) return;

            // repath per RePathCoolDown(15) ticks
            if (_repathCooldown > 0)
            {
                _repathCooldown--;
                return;
            }

            // warp if different location
            Farmer player = Game1.player;
            GameLocation location = pet.currentLocation;
            if (player.currentLocation != location)
            {
                this.WarpPet(pet, player.currentLocation, false);
                return;
            }

            if (PetState.State == PetStateEnum.Attacking)
            {
                pet.Halt();
                return; 
            }

            if (PetState.State == PetStateEnum.Combat)
            {
                attackMode = true;
            } else
            {
                attackMode = false;
            }

            // if stucked for 1 second and player is moving
            if (pet.TilePoint == _lastTile &&
                _playerLastPosition != player.Position && pet.controller != null)
            {
                stuckCounter += 2; // player move slow, count faster
            }
            else if (pet.TilePoint == _lastTile &&
                IsCharacterFarAway(player, pet) && pet.controller != null)
            {
                stuckCounter++;
            }
            else
            {
                stuckCounter = 0;
                _lastTile = pet.TilePoint;
            }
            _playerLastPosition = player.Position;

            DebugLog($"Pet stuck counter {stuckCounter}", LogLevel.Trace);
            
            // need to follow more closely in mine
            if (attackMode && stuckCounter > 30)
            {
                OnStuck();
                return;
            } else if (stuckCounter > 60 && IsCharacterFarAway(player, pet))
            {
                OnStuck();
                return;
            }

            // check if destination is new & far
            bool isMonster;
            Point? destination = FindDestinationForPet(pet, out isMonster);

            if (destination is null) return;

            bool isPathFound = false;
            if (isMonster)
            {
                DebugLog("Going for Monster", LogLevel.Debug);
                isPathFound = findPathForPet(pet, destination.Value,
                    Utilities.GetDirectionFromTileToTile(pet.TilePoint, destination.Value));
            } else
            {
                isPathFound = findPathForPet(pet, destination.Value);
            }

            if (isPathFound)
            {
                _lastDestination = destination;
                _repathCooldown = RePathCoolDown;
                _noPathFoundWait = 0;
            }
            else
            {
                // warp only if cannot find path for a while
                ++_noPathFoundWait;
                if (_noPathFoundWait > GetConfig!().TimeToWarpWhenNoPathFound)
                {
                    WarpPet(pet, player.currentLocation);
                    _noPathFoundWait = 0;
                }
            }

        }

        public void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (pet == null) return;
            WarpPet(pet, e.NewLocation, false);
        }

        private Point? FindDestinationForPet(Pet pet, out bool IsMonster)
        {
            IsMonster = false;
            Farmer player = Game1.player;
            Point? playerDestination = Utilities.GetTileBehindPlayer(pet, player, player.currentLocation);
            // already found way to player
            if (pet.controller != null && _lastDestination == playerDestination)
            {
                playerDestination = null;
            }

            // attack priority in mine
            if (attackMode)
            {
                // player very far
                if (Utilities.IsCharacterFarAway(player, pet, PlayerTooFarDistance))
                {
                    DebugLog($"far from player in mine", LogLevel.Trace);
                    return playerDestination;
                }
                // found monster
                if (HandleAttackDestination(pet.currentLocation) is Point point)
                {
                    IsMonster = true;
                    return point;
                } 
            }

            // didn't find monster, follow normal procedure
            if (IsCharacterFarAway(player, pet))
            {
                return playerDestination;
            }

            return null; // close enough, no need to move
        }
        private bool findPathForPet(Pet pet, Point destination)
        {
            return findPathForPet(pet, destination, Game1.player.facingDirection.Get());
        }
        private bool findPathForPet(Pet pet, Point destination, int direction)
        {
            TakeControlOfPet(pet);

            Stack<Point> path = PetPathFinding.findPath(pet.TilePoint, destination, IsAdjacentToEnd,
                pet.currentLocation, pet, 500);

            if (path == null)
            {
                DebugLog("No path found for pet " + pet.Name + " to destination " + destination, LogLevel.Trace);
                return false;
            }

            PathFindController pathFindController = new PathFindController(path, pet, pet.currentLocation)
            {
                finalFacingDirection = direction,
                NPCSchedule = false
            };

            pet.controller = pathFindController;

            pet.addedSpeed = GetConfig!().AddedFollowSpeed;  // faster to catch up player

            DebugLog($"Found path for {pet.Name}. Destination: {destination}", LogLevel.Trace);


            return true;

        }

        private void TakeControlOfPet(Pet pet)
        {
            pet.controller = null;

            pet.Halt();

            pet.Sprite?.ClearAnimation();

            pet.isSleepingOnFarmerBed.Value = false;

        }

        // handle warp, different location / map

        private void WarpPet(Pet pet, GameLocation newLocation, bool jump = true)
        {
            if (jump && GetConfig!().SoundOnJumpPet)
            {
                pet.jump();
            }
            else if (jump)
            {
                pet.jumpWithoutSound();
            }
            Game1.warpCharacter(pet, newLocation.NameOrUniqueName, Utilities.GetTileBehindPlayer(pet, Game1.player, newLocation));

            Monitor.Log($"Warped pet {pet.Name} to {newLocation.NameOrUniqueName}.", LogLevel.Trace);
        }

        private bool IsCharacterFarAway(Character character, Pet pet)
        {
            return Utilities.IsCharacterFarAway(character, pet, GetConfig!().FollowDistance);
        }


        private bool IsAdjacentToEnd(PathNode currentNode, Point endPoint, GameLocation location, Character c)
        {
            if (Math.Abs((currentNode.x - endPoint.X)) <= 1 &&
                Math.Abs((currentNode.y - endPoint.Y)) <= 1)
                return true;
            return false;
        }

        private Point? HandleAttackDestination(GameLocation location)
        {
            NetCollection<NPC> characters = location.characters;

            for (int num = characters.Count - 1; num >= 0; num--)
            {
                if (characters[num] is Monster { IsMonster: not false, Health: > 0 } monster)
                {
                    if (Utilities.IsCharacterFarAway(monster, pet, DetactMonsterDistance))
                    {
                        continue;
                    }
                    // monster is close
                    DebugLog($"found monster! " +
                            $"Distance: {Utilities.TileDistance(monster.TilePoint, pet.TilePoint)}", LogLevel.Debug);
                    return monster.TilePoint;
                }
            }
            return null;
        }

        private void OnStuck()
        {
            Farmer player = Game1.player;
            DebugLog($"Pet {pet.Name} seems stuck, warping to player.", LogLevel.Warn);
            this.WarpPet(pet, player.currentLocation);
            stuckCounter = 0;
        }
    }
}
