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
        private readonly Pet pet;
        private readonly PetState PetState;
        private readonly IMonitor Monitor;
        private readonly Func<ModConfig>? GetConfig;

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
        //

        public PetMove(IMonitor monitor, Func<ModConfig>? getConfig, Pet pet, PetState state)
        {
            Monitor = monitor;
            GetConfig = getConfig;
            this.pet = pet;
            this.PetState = state;
            PetPathFinding.initialize(GetConfig);
        }

        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e, Farmer owner)
        {
            // host handle warp, repath, etc.
            if (!Context.IsMainPlayer)
                return;

            // warp if different location
            GameLocation? location = pet.currentLocation;

            if (location is null || owner.currentLocation is null)
                return;

            if (owner.currentLocation != location)
            {
                WarpPet(owner, jump: false);
                return;
            }

            // repath per RePathCoolDown(15) ticks
            if (_repathCooldown > 0)
            {
                _repathCooldown--;
                return;
            }

            if (location is not MineShaft && PetState.State != PetStateEnum.Defeated)
            {
                PetState.SetState(PetStateEnum.Following);
            }

            if (PetState.State == PetStateEnum.Attacking)
            {
                pet.Halt();
                pet.controller = null;
                return; 
            }

            if (PetState.State == PetStateEnum.Combat)
            {
                attackMode = true;
            } else
            {
                attackMode = false;
            }

            updateStuckCounter(owner);

            if (CheckStuck(owner)) 
            {
                OnStuck(owner);
                return;
            }

            // check if destination is new & far
            bool isMonster;
            Point? destination = FindDestinationForPet(owner, out isMonster);

            if (destination is null) return;

            bool isPathFound = false;
            if (isMonster)
            {
                Monitor.VerboseLog($"{pet.name} Going for Monster");
                isPathFound = findPathForPet(pet, destination.Value,
                    Utilities.GetDirectionFromTileToTile(pet.TilePoint, destination.Value));
            } else
            {
                isPathFound = findPathForPet(pet, destination.Value, owner.facingDirection.Get());
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
                    WarpPet(owner);
                    _noPathFoundWait = 0;
                }
            }

        }

        public void OnOwnerWarped(Farmer owner)
        {
            if (!Context.IsMainPlayer
                || owner.currentLocation is null
                || pet.currentLocation == owner.currentLocation)
            {
                return;
            }

            WarpPet(owner, jump: false);
        }

        /// <summary>
        /// find destination for pet, near player or to monster
        /// </summary>
        /// <param name="pet"></param>
        /// <param name="IsMonster">if destination is to monster</param>
        /// <returns></returns>
        private Point? FindDestinationForPet(Farmer player, out bool IsMonster)
        {
            IsMonster = false;
            Point? playerDestination = Utilities.GetClosestValidTile(pet, player.TilePoint, player.currentLocation);
            // already found way to player
            if (pet.controller != null && _lastDestination == playerDestination)
            {
                playerDestination = null;
            }

            // attack priority in mine
            if (attackMode)
            {
                // player very far
                int PlayerTooFarDistance = GetConfig!().FollowDistance + 5;
                if (Utilities.IsCharacterFarAway(player, pet, PlayerTooFarDistance))
                {
                    Monitor.VerboseLog($"far from player in mine");
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

        /// <summary>
        /// try to find path, and set up controller for pet
        /// </summary>
        /// <param name="pet"></param>
        /// <param name="destination"></param>
        /// <param name="direction">facing direction</param>
        /// <returns></returns>
        private bool findPathForPet(Pet pet, Point destination, int direction)
        {
            TakeControlOfPet(pet);
            
            Stack<Point> path = PetPathFinding.findPath(pet.TilePoint, destination, IsAdjacentToEnd,
                pet.currentLocation, pet, 500);

            if (path == null)
            {
                Monitor.VerboseLog("No path found for pet " + pet.Name + " to destination " + destination);
                return false;
            }

            pet.controller = new PetPathFindController(Monitor, pet, pet.currentLocation, destination, direction, path);

            pet.addedSpeed = GetConfig!().AddedFollowSpeed;  // faster to catch up player

            Monitor.VerboseLog($"Found path for {pet.Name}. Destination: {destination}");


            return true;

        }

        private void TakeControlOfPet(Pet pet)
        {
            pet.controller = null;

            pet.Halt();

            pet.Sprite?.ClearAnimation();

            pet.isSleepingOnFarmerBed.Value = false;

        }

        private void ClearRepathState()
        {
            _lastDestination = null;
            _repathCooldown = 0;
            _noPathFoundWait = 0;
            stuckCounter = 0;
        }

        /// <summary>
        /// handle warp to new location
        /// </summary>
        /// <param name="pet"></param>
        /// <param name="newLocation"></param>
        /// <param name="jump"></param>
        private void WarpPet(Farmer owner, bool jump = true)
        {
            TakeControlOfPet(pet);
            ClearRepathState();

            GameLocation newLocation = owner.currentLocation;
            if (jump && GetConfig!().SoundOnJumpPet)
            {
                pet.jump();
            }
            else if (jump)
            {
                pet.jumpWithoutSound();
            }

            Point destination = Utilities.GetClosestValidTile(pet, owner.TilePoint, newLocation);
            Game1.warpCharacter(pet, newLocation.NameOrUniqueName, destination);

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

        /// <summary>
        /// Return a nearby monster location if any
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private Point? HandleAttackDestination(GameLocation location)
        {
            NetCollection<NPC> characters = location.characters;

            for (int num = characters.Count - 1; num >= 0; num--)
            {
                if (characters[num] is Monster { IsMonster: not false, Health: > 0 } monster)
                {
                    int DetactMonsterDistance = GetConfig!().FollowDistance + 4;
                    if (Utilities.IsCharacterFarAway(monster, pet, DetactMonsterDistance))
                    {
                        continue;
                    }
                    // monster is close
                    Monitor.VerboseLog($"{pet.name} found monster! " + $"Distance: {Utilities.TileDistance(monster.TilePoint, pet.TilePoint)}");
                    return monster.TilePoint;
                }
            }
            return null;
        }

        private void OnStuck(Farmer player)
        {
            Monitor.VerboseLog($"Pet {pet.Name} seems stuck, warping to player {player.name}.");
            this.WarpPet(player);
            stuckCounter = 0;
        }

        private bool CheckStuck(Farmer player)
        {
            // need to follow more closely in mine
            if ((attackMode && stuckCounter > 30) || (stuckCounter > 60 && IsCharacterFarAway(player, pet)))
            {
                return true;
            }
            return false;
        }

        private void updateStuckCounter(Farmer player)
        {
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
        }
    }
}
