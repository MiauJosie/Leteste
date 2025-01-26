using System;
using Leteste.Graphics;
using Leteste.Levels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Leteste.Physics;

// Represents a physical entity in the game world that can move and collide.
// Based on the Celeste/TowerFall physics system where all collision boxes are AABBs.
public class Actor
{
    // Core properties
    public Vector2 Position;
    public int Width;
    public int Height;
    public Level Level;

    // Rendering components
    public Sprite sprite;
    public Rectangle? sourceRect;
    protected bool isFacingRight = true;
    public float depth = 0f;

    // Physics state
    public float xRemainder;
    public float yRemainder;
    protected bool IsCollidable;

    // Collision box properties
    public int Left => GetBounds().Left;
    public int Right => GetBounds().Right;
    public int Top => GetBounds().Top;
    public int Bottom => GetBounds().Bottom;

    public Actor(Level level, Vector2 position, int width, int height)
    {
        Level = level;
        Position = position;
        Width = width;
        Height = height;
        IsCollidable = true;
    }

    // Moves the actor vertically with collision detection
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
                if (!CollideAt(Position + new Vector2(sign, 0)))
                {
                    Position.X += sign;
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

    // Same as MoveX but vertically
    public void MoveY(float amount, Action onCollide)
    {
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

    // Gets the collision bounds of the actor
    public virtual Rectangle GetBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            Width,
            Height
        );
    }

    // Checks for collision at a specific position without actually moving the actor
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

    // Determines if this actor is riding on a solid. Used for moving platform physics
    public virtual bool IsRiding(Solid solid)
    {
        var bounds = GetBounds();
        var solidBounds = solid.GetBounds();

        return bounds.Bottom == solidBounds.Top &&
               bounds.Left < solidBounds.Right &&
               bounds.Right > solidBounds.Left;
    }

    // Flips the sprite
    protected void SetFacing(bool right)
    {
        isFacingRight = right;
        if (sprite != null)
        {
            sprite.SetEffects(right ? SpriteEffects.None : SpriteEffects.FlipHorizontally);
        }
    }

    // Called when the actor is squeezed between solids
    public virtual void Squish()
    {
        Level.RemoveActor(this);
    }

    public virtual void Update()
    {
        // Base update logic
    }

    public virtual void Draw()
    {
        if (sprite != null)
        {
            sprite.Draw(Position, sourceRect);
        }
    }
}