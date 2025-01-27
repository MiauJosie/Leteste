using Leteste.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Leteste.Graphics;

/// <summary>
/// Represents a drawable sprite with various rendering properties.
/// Handles texture loading, origin positioning, and sprite transformations.
/// </summary>
public class Sprite
{
    // Core rendering properties
    private Texture2D texture;           // The sprite's texture
    private Vector2 origin;              // Point around which rotation occurs
    private Color color;                 // Tint color applied to the sprite
    private float rotation;              // Rotation in radians
    private float scale;                 // Uniform scale factor
    private SpriteEffects effects;       // Flip/mirror effects
    private Rectangle? sourceRect;       // Source rectangle for sprite sheets

    /// Convenience properties for sprite dimensions
    /// <summary>
    /// Gets the width of the sprite, accounting for source rectangle if present
    /// </summary>
    public int Width => sourceRect?.Width ?? texture.Width;

    /// <summary>
    /// Gets the height of the sprite, accounting for source rectangle if present
    /// </summary>
    public int Height => sourceRect?.Height ?? texture.Height;

    /// <summary>
    /// Creates a new Sprite with the specified texture
    /// </summary>
    /// <param name="texturePath">Path to the texture asset</param>
    /// <param name="centerOrigin">If true, sets origin to sprite center</param>
    public Sprite(string texturePath, bool centerOrigin = false)
    {
        // Load texture and initialize default properties
        texture = Globals.Content.Load<Texture2D>(texturePath);
        color = Color.White;
        rotation = 0f;
        scale = 1f;
        effects = SpriteEffects.None;
        sourceRect = null;

        // Set origin based on centerOrigin parameter
        origin = centerOrigin ? new Vector2(Width / 2, Height / 2) : Vector2.Zero;
    }

    /// <summary>
    /// Draws the sprite with optional overrides for rendering properties
    /// </summary>
    /// <param name="position">Position to draw the sprite</param>
    /// <param name="sourceRect">Optional source rectangle for sprite sheets</param>
    /// <param name="overrideColor">Optional color override</param>
    /// <param name="rotation">Optional rotation override</param>
    /// <param name="origin">Optional origin override</param>
    /// <param name="scale">Optional scale override</param>
    /// <param name="effects">Optional effects override</param>
    /// <param name="layerDepth">Optional depth layer (0=front, 1=back)</param>
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
            // Update origin if centered to match new dimensions
            if (origin == null && this.origin != Vector2.Zero)
            {
                this.origin = new Vector2(Width / 2, Height / 2);
            }
        }

        // Draw the sprite using provided or default values
        Globals.SpriteBatch.Draw(
            texture,
            position,
            sourceRect ?? this.sourceRect,
            overrideColor ?? color,
            rotation + this.rotation,     // Combine provided and stored rotation
            origin ?? this.origin,        // Use provided origin or default
            scale * this.scale,           // Combine provided and stored scale
            effects | this.effects,       // Combine provided and stored effects
            layerDepth
        );
    }

    // Helper methods to modify sprite properties
    /// <summary>Sets the tint color of the sprite</summary>
    public void SetColor(Color newColor) => color = newColor;

    /// <summary>Sets the rotation of the sprite in radians</summary>
    public void SetRotation(float newRotation) => rotation = newRotation;

    /// <summary>Sets the uniform scale factor of the sprite</summary>
    public void SetScale(float newScale) => scale = newScale;

    /// <summary>Sets the flip/mirror effects of the sprite</summary>
    public void SetEffects(SpriteEffects newEffects) => effects = newEffects;

    /// <summary>
    /// Sets the source rectangle for sprite sheet animation
    /// Updates origin if sprite is center-aligned
    /// </summary>
    public void SetSourceRect(Rectangle? rect)
    {
        sourceRect = rect;
        // Update origin if centered to match new dimensions
        if (origin != Vector2.Zero)
        {
            origin = new Vector2(Width / 2, Height / 2);
        }
    }
}