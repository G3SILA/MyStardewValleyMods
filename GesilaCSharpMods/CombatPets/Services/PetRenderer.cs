using StardewValley;
using StardewValley.Characters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Locations;

namespace CombatPets
{
    internal class PetRenderer
    {
        private readonly IMonitor Monitor;
        private readonly Func<ModConfig> GetConfig;
        private readonly Pet pet; 
        private readonly PetState PetState;

        public PetRenderer(IMonitor monitor, Func<ModConfig> getconfig, Pet pet, PetState state)
        {
            Monitor = monitor;
            GetConfig = getconfig;
            this.pet = pet;
            this.PetState = state;
        }

        public void OnRendered(object? sender, RenderedEventArgs e)
        {

            if (pet.currentLocation != Game1.currentLocation)
                return;

            if (GetConfig().ShowHealthBar == ShowHealthBar.Always)
            {
                if (PetState.State == PetStateEnum.Combat || PetState.State == PetStateEnum.Attacking)
                {
                    DrawPetHealthBar(e.SpriteBatch,
                    pet.getLocalPosition(Game1.viewport) + new Vector2(0, -pet.GetBoundingBox().Height));
                }
            } else if (GetConfig().ShowHealthBar == ShowHealthBar.InCombat)
            {
                if (PetState.IsAttacked())
                {
                    DrawPetHealthBar(e.SpriteBatch,
                        pet.getLocalPosition(Game1.viewport) + new Vector2(0, -pet.GetBoundingBox().Height));
                }
            }
            

            if (PetState.InvincibleCountDown % 10 > 5)
            {
                GlowEffect(e.SpriteBatch, Color.Red, 0.5f);
            }
        
        }

        private void DrawPetHealthBar(SpriteBatch spriteBatch, Vector2 position)
        {
            int barWidth = 50;
            int barHeight = 5;
            float healthPercentage = (float)PetState.Health / PetState.MaxHealth;

            position += new Vector2((pet.Sprite.SpriteWidth * 4 - barWidth) / 2, 0);

            // Draw background
            spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)position.X, (int)position.Y - 10, barWidth, barHeight), Color.Gray);
            // Draw health
            spriteBatch.Draw(Game1.staminaRect, new Rectangle((int)position.X, (int)position.Y - 10, (int)(barWidth * healthPercentage), barHeight), Color.Red);
        }

        private void GlowEffect(SpriteBatch b, Color color, float transparency)
        {
            if (!pet.currentLocation.Equals(Game1.currentLocation))
                return;
            
            int standingY = pet.StandingPixel.Y;

            Vector2 shake = pet.shakeTimer > 0? 
                new Vector2(Game1.random.Next(-1, 2), Game1.random.Next(-1, 2))
                : Vector2.Zero;

            Vector2 drawPosition = pet.getLocalPosition(Game1.viewport) + 
                new Vector2(pet.Sprite.SpriteWidth * 4 / 2, pet.GetBoundingBox().Height / 2) + shake;

            Vector2 origin = new Vector2(pet.Sprite.SpriteWidth / 2f, pet.Sprite.SpriteHeight * 3f / 4f);

            SpriteEffects effects = pet.flip || (pet.Sprite.CurrentAnimation != null && pet.Sprite.CurrentAnimation[pet.Sprite.currentAnimationIndex].flip)
                    ? SpriteEffects.FlipHorizontally
                    : SpriteEffects.None;

            float layerDepth = Math.Max(0f, pet.isSleepingOnFarmerBed.Value? 
                (standingY + 112f) / 10000f: standingY / 10000f);

            b.Draw(
                pet.Sprite.Texture,
                drawPosition,
                pet.Sprite.SourceRect,
                color * transparency,
                pet.rotation,
                origin,
                Math.Max(0.2f, pet.scale.Value) * 4f,
                effects,
                layerDepth + 0.0001f
            );
        }
    }
}
