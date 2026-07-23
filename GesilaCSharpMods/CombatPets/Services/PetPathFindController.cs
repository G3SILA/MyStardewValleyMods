using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.Locations;
using StardewValley;
using StardewValley.Pathfinding;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace CombatPets
{
    internal class PetPathFindController : PathFindController
    {
        Character character;
        IMonitor Monitor;
        public PetPathFindController(IMonitor monitor, Character character, GameLocation location, Point end, int endFacingDirection, Stack<Point> path)
            : base(path, character, location)
        {
            this.character = character;
            this.Monitor = monitor;
            finalFacingDirection = endFacingDirection;
            NPCSchedule = false;
        }

        // modified from StardewValley.Pathfinding.PathFindController.moveCharacter
        protected override void moveCharacter(GameTime time)
        {
            Point point = pathToEndPoint.Peek();
            Rectangle rectangle = new Rectangle(point.X * 64, point.Y * 64, 64, 64);
            rectangle.Inflate(-2, 0);

            // use the farmer width to check for passable tiles, since pets have larger bounding boxes than the farmer
            // allow for pets to pass
            Rectangle boundingBox = character.GetBoundingBox();
            boundingBox.Width = Game1.player.GetBoundingBox().Width;
            if ((rectangle.Contains(boundingBox) || (boundingBox.Width > rectangle.Width && rectangle.Contains(boundingBox.Center))) && rectangle.Bottom - boundingBox.Bottom >= 2)
            {
                timerSinceLastCheckPoint = 0;
                pathToEndPoint.Pop();
                character.stopWithoutChangingFrame();
                if (pathToEndPoint.Count == 0)
                {
                    character.Halt();
                    if (finalFacingDirection != -1)
                    {
                        character.faceDirection(finalFacingDirection);
                    }

                    if (NPCSchedule)
                    {
                        NPC nPC = character as NPC;
                        nPC.DirectionsToNewLocation = null;
                        nPC.endOfRouteMessage.Value = nPC.nextEndOfRouteMessage;
                    }

                    endBehaviorFunction?.Invoke(character, location);
                }

                return;
            }

            if (character is Farmer farmer)
            {
                farmer.movementDirections.Clear();
            }
            else if (!(location is MovieTheater))
            {
                string name = character.Name;
                for (int i = 0; i < location.characters.Count; i++)
                {
                    NPC nPC2 = location.characters[i];
                    if (!nPC2.Equals(character) && nPC2.GetBoundingBox().Intersects(boundingBox) && nPC2.isMoving() && string.Compare(nPC2.Name, name, StringComparison.Ordinal) < 0)
                    {
                        character.Halt();
                        return;
                    }
                }
            }

            if (boundingBox.Left < rectangle.Left && boundingBox.Right < rectangle.Right)
            {
                character.SetMovingRight(b: true);
            }
            else if (boundingBox.Right > rectangle.Right && boundingBox.Left > rectangle.Left)
            {
                character.SetMovingLeft(b: true);
            }
            else if (boundingBox.Top <= rectangle.Top)
            {
                character.SetMovingDown(b: true);
            }
            else if (boundingBox.Bottom >= rectangle.Bottom - 2)
            {
                character.SetMovingUp(b: true);
            }

            character.MovePosition(time, Game1.viewport, location);
            if (nonDestructivePathing)
            {
                if (rectangle.Intersects(character.nextPosition(character.FacingDirection)))
                {
                    Vector2 vector = character.nextPositionVector2();
                    StardewValley.Object objectAt = location.getObjectAt((int)vector.X, (int)vector.Y);
                    if (objectAt != null)
                    {
                        if (objectAt is Fence fence && fence.isGate.Value)
                        {
                            fence.toggleGate(open: true);
                        }
                        else if (!objectAt.isPassable())
                        {
                            character.Halt();
                            character.controller = null;
                            return;
                        }
                    }
                }

                handleWarps(character.nextPosition(character.getDirection()));
            }
            else if (NPCSchedule)
            {
                handleWarps(character.nextPosition(character.getDirection()));
            }
        }
    }
}
