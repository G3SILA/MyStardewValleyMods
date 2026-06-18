using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Pathfinding;
using Microsoft.Xna.Framework;

namespace CombatPets
{
    internal class PetMove
    {
        public Pet pet; 
        private IMonitor Monitor;
        private ModConfig Config;

        private Point? _lastDestination; 
        private int _repathCooldown = 0; 

        public PetMove(IMonitor monitor, ModConfig config) 
        { 
            Monitor = monitor;
            Config = config;
        }

        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (pet == null) return;

            if (_repathCooldown > 0)
            {
                _repathCooldown--;
                return;
            }


            Point? destination = FindDestinationForPet(pet);
            if (destination == null) return;

            Farmer player = Game1.player;
            if (pet.controller != null && _lastDestination == destination) return;

            // new destination, find path
            Monitor.Log($"destination found {destination}.", LogLevel.Debug);

            if (findPathForPet(pet, destination.Value))
            {
                _lastDestination = destination;
                _repathCooldown = 15;
            }

        }

        public void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (pet == null) return;
            pet.currentLocation = e.NewLocation;

        }

        private Point? FindDestinationForPet(Pet pet)
        {

            Farmer player = Game1.player;
            if (player.currentLocation != pet.currentLocation)
            {
                return null; // different location, handle in OnWarped
            }

            if (IsFarmerFarAway(player, pet))
            {
                // just for now, always back of player
                return GetTileBehindPlayer();
            }

            return null; // close enough, no need to move
        }

        private bool findPathForPet(Pet pet, Point destination)
        {

            Stack<Point>? path = PathFindController.findPath(pet.TilePoint, destination, IsAdjacentToEnd, pet.currentLocation,pet,5000);

            if (path == null || path.Count == 0) {
                Monitor.Log($"No path found for pet {pet.Name} to destination {destination}.", LogLevel.Debug);
                Monitor.Log($"Pet position: {pet.TilePoint}, Player position: {Game1.player.TilePoint}", LogLevel.Debug);
                return false;
            }

            // found path, assign to pet controller
            PathFindController pathFindController = new PathFindController(pet, pet.currentLocation, destination, Game1.player.facingDirection.Get());

            pathFindController.NPCSchedule = false;

            pathFindController.pathToEndPoint = path;

            pet.controller = pathFindController;

            Monitor.Log($"Found path for {pet.Name}. Steps: {path.Count}, Destination: {destination}", LogLevel.Debug);

            return true;

        }


        /*
            random helpers 
        */
        private bool IsFarmerFarAway(Farmer farmer, Pet pet)    
        {   
            if (pet == null) return false;
            return TileDistance(farmer.TilePoint, pet.TilePoint) > Config.FollowDistance;
        }
        private int TileDistance(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        private bool IsAdjacentToEnd(PathNode currentNode, Point endPoint, GameLocation location, Character c)
        {
            if (Math.Abs((currentNode.x - endPoint.X)) < 2 &&
                Math.Abs((currentNode.y - endPoint.Y)) < 2)
                return true;
            return false;
        }

        private Point GetTileBehindPlayer()
        {
            Farmer player = Game1.player;
            Point tile = player.TilePoint;
            
            return player.FacingDirection switch
            {
                0 => new Point(tile.X, tile.Y + 1), // player facing up, pet goes below
                1 => new Point(tile.X - 1, tile.Y), // player facing right, pet goes left
                2 => new Point(tile.X, tile.Y - 1), // player facing down, pet goes above
                3 => new Point(tile.X + 1, tile.Y), // player facing left, pet goes right
                _ => tile
            };
        }

    }
}
