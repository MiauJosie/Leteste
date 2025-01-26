using Leteste.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Leteste.Graphics;

public class Sprite
{
    private Texture2D texture;
    private Vector2 origin;
    private Color color;
    private float rotation;
    private float scale;
    private SpriteEffects effects;
    private Rectangle? sourceRect;

    public int Width => sourceRect?.Width ?? texture.Width;
    public int Height => sourceRect?.Height ?? texture.Height;

    public Sprite(string texturePath, bool centerOrigin = false)
    {
        texture = Globals.Content.Load<Texture2D>(texturePath);
        color = Color.White;
        rotation = 0f;
        scale = 1f;
        effects = SpriteEffects.None;
        sourceRect = null;

        // Now origin is set after Width/Height properties are available
        origin = centerOrigin ? new Vector2(Width / 2, Height / 2) : Vector2.Zero;
    }

    public void Draw(
        Vector2 position,
        Rectangle? sourceRect = null,
        Color? overrideColor = null,
        float rotation = 0f,
        Vector2? origin = null,
        float scale = 1f,
        SpriteEffects effects = SpriteEffects.None,
        float layerDepth = 0f
    )
    {
        // Update the source rectangle if provided
        if (sourceRect.HasValue)
        {
            this.sourceRect = sourceRect;
            // Update origin if centered
            if (origin == null && this.origin != Vector2.Zero)
            {
                this.origin = new Vector2(Width / 2, Height / 2);
            }
        }

        Globals.SpriteBatch.Draw(
            texture,
            position,
            sourceRect ?? this.sourceRect,
            overrideColor ?? color,
            rotation + this.rotation,
            origin ?? this.origin,
            scale * this.scale,
            effects | this.effects,
            layerDepth
        );
    }

    // Helper methods to modify sprite properties
    public void SetColor(Color newColor) => color = newColor;
    public void SetRotation(float newRotation) => rotation = newRotation;
    public void SetScale(float newScale) => scale = newScale;
    public void SetEffects(SpriteEffects newEffects) => effects = newEffects;
    public void SetSourceRect(Rectangle? rect)
    {
        sourceRect = rect;
        // Update origin if centered
        if (origin != Vector2.Zero)
        {
            origin = new Vector2(Width / 2, Height / 2);
        }
    }
}