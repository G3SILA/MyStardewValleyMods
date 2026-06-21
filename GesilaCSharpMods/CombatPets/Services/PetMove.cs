using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Pathfinding;


namespace CombatPets
{
    internal class PetMove
    {
        public Pet pet; 
        private IMonitor Monitor;
        private ModConfig Config;

        private Point? _lastDestination; 
        private int _repathCooldown = 0;

        private Point _lastTile;
        private Vector2 _playerLastPosition;
        private int stuckCounter = 0;

        public PetMove(IMonitor monitor, ModConfig config) 
        { 
            Monitor = monitor;
            Config = config;
        }

        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (pet == null) return;

            // repath per 15 ticks
            if (_repathCooldown > 0)
            {
                _repathCooldown--;
                return;
            }

            // warp if different location
            Farmer player = Game1.player;
            if (player.currentLocation != pet.currentLocation)
            {
                this.WarpPet(pet, player.currentLocation);
                return;
            }

            // if stucked for 1 second and player is moving
            if (pet.TilePoint == _lastTile && 
                _playerLastPosition != player.Position && pet.controller != null)
            {
                stuckCounter += 2; // player move slow, count faster

                Monitor.Log($"{stuckCounter}, player: {player.Position}", LogLevel.Debug);
            
                if (stuckCounter > 60) 
                {
                    Monitor.Log($"Pet {pet.Name} seems stuck, warping to player.", LogLevel.Warn);
                    this.WarpPet(pet, player.currentLocation);
                    stuckCounter = 0;
                    return;
                }

                // test
                Rectangle box = pet.GetBoundingBox();

                bool colliding = pet.currentLocation.isCollidingPosition(
                    box,
                    Game1.viewport,
                    false,
                    0,
                    false,
                    pet
                );

                Rectangle playerBox = Game1.player.GetBoundingBox();

                Monitor.Log(
                    $"Player pos={player.Position}, tile={player.TilePoint}, box={playerBox}, " +
                    $"Pet pos={pet.Position}, tile={pet.TilePoint}, " +
                    $"box={box}, my_box = {Utilities.GetRelativeBoundingBox(pet,pet.TilePoint)}, colliding={colliding}, " +
                    $"controller={(pet.controller == null ? "null" : "active")}, " +
                    $"moving={pet.isMoving()}",
                    LogLevel.Debug
                );
                // end test
            }
            else if (pet.TilePoint == _lastTile && 
                IsFarmerFarAway(player, pet) && pet.controller != null)
            {
                stuckCounter++; 
            }
            else
            {
                stuckCounter = 0;
                _lastTile = pet.TilePoint;
            }
            _playerLastPosition = player.Position;
            
            
            // check if destination is new & far
            Point? destination = FindDestinationForPet(pet);
            if (destination == null) return;

            if (pet.controller != null && _lastDestination == destination) return;


            if (findPathForPet(pet, destination.Value))
            {
                _lastDestination = destination;
                _repathCooldown = 15;
            }

        }

        public void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (pet == null) return;
            WarpPet(pet, e.NewLocation);
        }

        private Point? FindDestinationForPet(Pet pet)
        {

            Farmer player = Game1.player;
            
            if (IsFarmerFarAway(player, pet))
            {
                return Utilities.GetTileBehindPlayer(pet, player, player.currentLocation);
            }

            return null; // close enough, no need to move
        }

        private bool findPathForPet(Pet pet, Point destination)
        {
            TakeControlOfPet(pet);

            PathFindController pathFindController = new PathFindController(pet, pet.currentLocation, destination, Game1.player.facingDirection.Get());

            pathFindController.NPCSchedule = false;

            
            pet.controller = pathFindController;
            pet.addedSpeed = 2f;  // faster to catch up player

            Monitor.Log($"Found path for {pet.Name}. Destination: {destination}", LogLevel.Trace);


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

        private void WarpPet(Pet pet, GameLocation newLocation)
        {
            Game1.warpCharacter(pet, newLocation.NameOrUniqueName, Utilities.GetTileBehindPlayer(pet, Game1.player, newLocation));

            Monitor.Log($"Warped pet {pet.Name} to {newLocation.NameOrUniqueName}.", LogLevel.Debug);
        }


        /*
            random helpers 
        */
        private bool IsFarmerFarAway(Farmer farmer, Pet pet)    
        {   
            if (pet == null) return false;
            return Utilities.TileDistance(farmer.TilePoint, pet.TilePoint) > Config.FollowDistance;
        }
        
        private bool IsAdjacentToEnd(PathNode currentNode, Point endPoint, GameLocation location, Character c)
        {
            if (Math.Abs((currentNode.x - endPoint.X)) < 2 &&
                Math.Abs((currentNode.y - endPoint.Y)) < 2)
                return true;
            return false;
        }


    }
}
