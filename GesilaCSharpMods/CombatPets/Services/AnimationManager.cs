using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;


namespace CombatPets
{
    internal class AnimationManager
    {
        private static IModHelper helper;
        private Texture2D _attackTexture = null!;
        private readonly Dictionary<Point, Texture2D> _debugRectTextures = new();
        public AnimationManager(IModHelper Ihelper)
        {
            helper = Ihelper;
            _attackTexture = helper.ModContent.Load<Texture2D>("assets/attack.png");
        }

        public AnimationManager() { }

        public void DrawAttack(GameLocation location, Rectangle area, bool flipped)
        {
            var attack = new TemporaryAnimatedSprite(
            initialParentTileIndex: 0,
            animationInterval: 70f,
            animationLength: 4,
            numberOfLoops: 1,
            position: new Vector2(area.X, area.Y),
            flicker: false,
            flipped: flipped
            )
            {
                scale = (area.Width/16f),
                texture = _attackTexture,
                sourceRect = new Rectangle(0, 0, 16, 16),
                sourceRectStartingPos = Vector2.Zero,
                alpha = 1f,
                layerDepth = 0f
            };

            location.temporarySprites.Add(attack);
        }

        // show the area once
        public void DebugArea(GameLocation location, Rectangle area)
        {
            DebugArea(location, area, Color.Red, 0.35f, 250f);
        }
        public void DebugArea(GameLocation location, Rectangle area, Color color, float alphaV, float animInterval)
        {
            Texture2D texture = GetDebugRectTexture(area.Width, area.Height);

            var sprite = new TemporaryAnimatedSprite(
                initialParentTileIndex: 0,
                animationInterval: animInterval,
                animationLength: 1,
                numberOfLoops: 1,
                position: new Vector2(area.X, area.Y),
                flicker: false,
                flipped: false
            )
            {
                texture = texture,
                sourceRect = new Rectangle(0, 0, area.Width, area.Height),
                sourceRectStartingPos = Vector2.Zero,

                color = color,
                alpha = alphaV,

                scale = 1f,
                layerDepth = 1f
            };

            location.temporarySprites.Add(sprite);
        }

        // get a white translucent texture
        private Texture2D GetDebugRectTexture(int width, int height)
        {
            Point size = new Point(width, height);

            if (_debugRectTextures.TryGetValue(size, out Texture2D texture))
                return texture;

            texture = new Texture2D(Game1.graphics.GraphicsDevice, width, height);

            Color[] data = new Color[width * height];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = Color.White;
            }

            texture.SetData(data);
            _debugRectTextures[size] = texture;

            return texture;
        }
    }
}
