
using StardewValley;
using StardewValley.Pathfinding;
using Microsoft.Xna.Framework;
using static StardewValley.Pathfinding.PathFindController;

namespace CombatPets
{
    // modified version of StardewValley.Pathfinding.PathFindController
    internal class PetPathFinding
    {
        protected static PriorityQueue _openList = new PriorityQueue();

        protected static HashSet<int> _closedList = new HashSet<int>();

        protected static int _counter = 0;

        static readonly sbyte[,] Directions = new sbyte[4, 2]
        {
            { -1, 0 },
            { 1, 0 },
            { 0, 1 },
            { 0, -1 }
        };

        private static ModConfig Config; 

        public static void initialize(ModConfig config)
        {
            Config = config;
        }

        public static Stack<Point> findPath(Point startPoint, Point endPoint, isAtEnd endPointFunction, GameLocation location, Character character, int limit)
        {
            if (Interlocked.Increment(ref _counter) != 1)
            {
                throw new Exception();
            }
            
            try
            {
                bool flag = character is FarmAnimal farmAnimal && farmAnimal.CanSwim() && farmAnimal.isSwimming.Value;
                _openList.Clear();
                _closedList.Clear();
                PriorityQueue openList = _openList;
                HashSet<int> closedList = _closedList;
                int num = 0;
                openList.Enqueue(new PathNode(startPoint.X, startPoint.Y, 0, null), Math.Abs(endPoint.X - startPoint.X) + Math.Abs(endPoint.Y - startPoint.Y));
                int layerWidth = location.map.Layers[0].LayerWidth;
                int layerHeight = location.map.Layers[0].LayerHeight;
                while (!openList.IsEmpty())
                {
                    PathNode pathNode = openList.Dequeue();
                    if (endPointFunction(pathNode, endPoint, location, character))
                    {
                        return reconstructPath(pathNode);
                    }

                    closedList.Add(pathNode.id);
                    int num2 = (byte)(pathNode.g + 1);
                    for (int i = 0; i < 4; i++)
                    {
                        int num3 = pathNode.x + Directions[i, 0];
                        int num4 = pathNode.y + Directions[i, 1];
                        int item = PathNode.ComputeHash(num3, num4);
                        if (closedList.Contains(item))
                        {
                            continue;
                        }

                        if ((num3 != endPoint.X || num4 != endPoint.Y) && (num3 < 0 || num4 < 0 || num3 >= layerWidth || num4 >= layerHeight))
                        {
                            closedList.Add(item);
                            continue;
                        }

                        PathNode pathNode2 = new PathNode(num3, num4, pathNode);
                        pathNode2.g = (byte)(pathNode.g + 1);

                        // modified part
                        Rectangle collisionBox;
                        if (Config.LargerCollisionEnabled)
                        {
                            collisionBox = new Rectangle(pathNode2.x * 64 - 16, pathNode2.y * 64 - 16, 100, 62);
                        } else
                        {
                            collisionBox = new Rectangle(pathNode2.x * 64, pathNode2.y * 64, 100, 62);
                        }
                                                
                        if (!flag && location.isCollidingPosition(collisionBox, Game1.viewport, character is Farmer, 0, glider: false, character, pathfinding: true))
                        {
                            closedList.Add(item);
                            continue;
                        }
                        //

                        int priority = num2 + (Math.Abs(endPoint.X - num3) + Math.Abs(endPoint.Y - num4));
                        closedList.Add(item);
                        openList.Enqueue(pathNode2, priority);
                    }

                    num++;
                    if (num >= limit)
                    {
                        return null;
                    }
                }

                return null;
            }
            finally
            {
                if (Interlocked.Decrement(ref _counter) != 0)
                {
                    throw new Exception();
                }
            }
        }
    }
}
