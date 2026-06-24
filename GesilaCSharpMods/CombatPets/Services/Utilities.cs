using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Pathfinding;

namespace CombatPets
{
    internal static class Utilities
    {
        public static int TileDistance(Point a, Point b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }
        public static bool CanPlacePetTile(Pet pet, Point tile, GameLocation location)
        {
            Vector2 tileVector = tile.ToVector2();

            Rectangle box = pet.GetBoundingBox();
            // collision? 
            bool colliding = location.isCollidingPosition(
                new Rectangle(tile.X * 64, tile.Y * 64, box.Width + 2, box.Height + 2), 
                Game1.viewport, false, 0, false, pet);

            if (colliding) return false;

            // other conditions
            return location.CanSpawnCharacterHere(tileVector);
        }

        /// <summary>
        /// get the tile behind player based on facing direction, if not valid,     return one of the valid adjavent tile, return player's tile if none is  valid
        /// </summary>
        /// <param name="pet"></param>
        /// <param name="player"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Point GetTileBehindPlayer(Pet pet, Farmer player, GameLocation location)
        {
            Point tile = player.TilePoint;

            Point expectPoint = player.FacingDirection switch
            {
                0 => new Point(tile.X, tile.Y + 1), // player facing up, pet goes below
                1 => new Point(tile.X - 1, tile.Y), // player facing right, pet goes left
                2 => new Point(tile.X, tile.Y - 1), // player facing down, pet goes above
                3 => new Point(tile.X + 1, tile.Y), // player facing left, pet goes right
                _ => tile
            };

            if (CanPlacePetTile(pet, expectPoint, location))
            {
                return expectPoint;
            }

            return GetAdjacentValidTile(pet, expectPoint, player.currentLocation) ?? tile;

        }

        /// <summary>
        /// return one of 8 valid adjacent tile for pet, else null
        /// </summary>
        /// <param name="pet"></param>
        /// <param name="tile"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public static Point? GetAdjacentValidTile(Pet pet, Point tile, GameLocation location)
        {
            List<Point> adjacentTiles = new List<Point>
            {
                new Point(tile.X + 1, tile.Y),
                new Point(tile.X - 1, tile.Y),
                new Point(tile.X, tile.Y + 1),
                new Point(tile.X, tile.Y - 1),
                new Point(tile.X + 1, tile.Y + 1),
                new Point(tile.X + 1, tile.Y - 1),
                new Point(tile.X - 1, tile.Y + 1),
                new Point(tile.X - 1, tile.Y - 1)
            };
            foreach (var adj in adjacentTiles)
            {
                if (CanPlacePetTile(pet, adj, location))
                {
                    return adj;
                }
            }
            return null;
        }

        /// <summary>
        /// Find the target tile's bounding box relative to the pet's bounding box.
        /// </summary>
        public static Rectangle GetRelativeBoundingBox(Pet pet, Point targetTile)
        {
            Rectangle petBox = pet.GetBoundingBox();
            
            Point boxOffset = new Point(
                petBox.X - (int)pet.Position.X,
                petBox.Y - (int)pet.Position.Y
            );

            Point pixelTarget = new Point(
                targetTile.X * Game1.tileSize,
                targetTile.Y * Game1.tileSize
            );

            Rectangle relativeBox = new Rectangle(
                pixelTarget.X + boxOffset.X,
                pixelTarget.Y + boxOffset.Y, 
                petBox.Width, petBox.Height);
            
            return relativeBox;
        }
    }
}
