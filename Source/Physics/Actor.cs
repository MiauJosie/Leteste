using System;
using Leteste.Graphics;
using Leteste.Levels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Leteste.Physics;

/// <summary>
/// Represents a physical entity in the game world that can move and collide.
/// Based on the Celeste/TowerFall physics system where all collision boxes are AABBs (Axis-Aligned Bounding Boxes).
/// </summary>
public class Actor
{
    // Core properties
    public Vector2 Position;              // Current position in world space
    public int Width;                     // Width of collision box
    public int Height;                    // Height of collision box
    public Level Level;                   // Reference to the current level

    // Rendering components
    public Sprite sprite;                 // Visual representation
    public Rectangle? sourceRect;         // Source rectangle for sprite sheet animations
    protected bool isFacingRight = true;  // Direction the actor is facing
    public float depth = 0f;              // Render depth/layer (lower values = further back)

    // Physics state
    public float xRemainder;              // Sub-pixel movement accumulator for X axis
    public float yRemainder;              // Sub-pixel movement accumulator for Y axis
    protected bool IsCollidable;          // Whether this actor can collide with solids

    // Collision box properties - convenience getters for bounds
    public int Left => GetBounds().Left;
    public int Right => GetBounds().Right;
    public int Top => GetBounds().Top;
    public int Bottom => GetBounds().Bottom;

    /// <summary>
    /// Creates a new Actor with specified dimensions and position
    /// </summary>
    public Actor(Level level, Vector2 position, int width, int height)
    {
        Level = level;
        Position = position;
        Width = width;
        Height = height;
        IsCollidable = true;
    }

    /// <summary>
    /// Moves the actor horizontally with pixel-perfect collision detection.
    /// Handles sub-pixel movement using remainder system.
    /// </summary>
    /// <param name="amount">Amount to move in pixels (can be fractional)</param>
    /// <param name="onCollide">Optional callback when collision occurs</param>
    public void MoveX(float amount, Action onCollide)
    {
        xRemainder += amount;
        int move = (int)MathF.Round(xRemainder);

        if (move != 0)
        {
            xRemainder -= move;
            int sign = Math.Sign(move);

            while (move != 0)
            {
                // Try to move one pixel at a time
                if (!CollideAt(Position + new Vector2(sign, 0)))
                {
                    Position.X += sign;
                    move -= sign;
                }
                else
                {
                    // Hit something, trigger collision callback
                    if (onCollide != null)
                    {
                        onCollide();
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Moves the actor vertically with pixel-perfect collision detection.
    /// Functions identically to MoveX but for vertical movement.
    /// </summary>
    public void MoveY(float amount, Action onCollide)
    {
        // Implementation mirrors MoveX but for Y axis
        yRemainder += amount;
        int move = (int)MathF.Round(yRemainder);

        if (move != 0)
        {
            yRemainder -= move;
            int sign = Math.Sign(move);

            while (move != 0)
            {
                if (!CollideAt(Position + new Vector2(0, sign)))
                {
                    Position.Y += sign;
                    move -= sign;
                }
                else
                {
                    if (onCollide != null)
                    {
                        onCollide();
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Gets the collision bounds of the actor
    /// Can be overridden by derived classes for custom collision boxes
    /// </summary>
    public virtual Rectangle GetBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            Width,
            Height
        );
    }

    /// <summary>
    /// Checks for collision at a specific position without moving the actor
    /// </summary>
    /// <param name="position">Position to test</param>
    /// <returns>True if collision would occur at the test position</returns>
    public virtual bool CollideAt(Vector2 position)
    {
        // Store original position
        Vector2 originalPos = Position;
        Position = position;

        // Get bounds at test position
        Rectangle bounds = GetBounds();

        // Restore position
        Position = originalPos;

        // Check collision against all collidable solids
        foreach (var solid in Level.GetSolids())
        {
            if (solid.Collidable && bounds.Intersects(solid.GetBounds()))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Determines if this actor is riding on top of a solid
    /// Used for moving platform physics and ground detection
    /// </summary>
    public virtual bool IsRiding(Solid solid)
    {
        var bounds = GetBounds();
        var solidBounds = solid.GetBounds();

        return bounds.Bottom == solidBounds.Top &&
               bounds.Left < solidBounds.Right &&
               bounds.Right > solidBounds.Left;
    }

    /// <summary>
    /// Updates the actor's facing direction and sprite effects
    /// </summary>
    protected void SetFacing(bool right)
    {
        isFacingRight = right;
        if (sprite != null)
        {
            sprite.SetEffects(right ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
        }
    }

    /// <summary>
    /// Called when the actor is squeezed between solids
    /// Default behavior is to remove the actor
    /// </summary>
    public virtual void Squish()
    {
        Level.RemoveActor(this);
    }

    /// <summary>
    /// Base update method for actor logic
    /// Should be overridden by derived classes
    /// </summary>
    public virtual void Update()
    {
        // Base update logic
    }

    /// <summary>
    /// Draws the actor's sprite if one exists
    /// </summary>
    public virtual void Draw()
    {
        if (sprite != null)
        {
            sprite.Draw(Position, sourceRect);
        }
    }
}