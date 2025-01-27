using System;
using System.Collections.Generic;
using System.Linq;
using Leteste.Graphics;
using Leteste.Levels;
using Microsoft.Xna.Framework;

namespace Leteste.Physics;

/// <summary>
/// Represents a solid object in the game world that other entities can collide with.
/// Can be static (like walls) or dynamic (like moving platforms).
/// Handles complex interactions with actors including carrying and pushing.
/// </summary>
public class Solid
{
    // Core properties
    public Vector2 Position;             // Current position in world space
    public bool Collidable;              // Whether other entities can collide with this solid
    public int Width;                    // Width of collision box
    public int Height;                   // Height of collision box
    public Level Level;                  // Reference to the current level
    public bool IsOneWay;                // If true, can only be collided with from above

    // Rendering components
    public Sprite sprite;                // Visual representation
    public Rectangle? sourceRect;        // Source rectangle for sprite sheet animations

    // Physics state
    public float yRemainder;             // Sub-pixel movement accumulator for Y axis
    public float xRemainder;             // Sub-pixel movement accumulator for X axis

    // Cached lists for performance
    public List<Actor> carriedActors = new();    // Actors currently riding this solid
    public List<Actor> collidingActors = new();  // Actors currently colliding with this solid

    // Collision box properties - convenience getters for bounds
    public int Left => GetBounds().Left;
    public int Right => GetBounds().Right;
    public int Top => GetBounds().Top;
    public int Bottom => GetBounds().Bottom;

    /// <summary>
    /// Creates a new Solid with specified dimensions and position
    /// </summary>
    public Solid(Level level, Vector2 position, int width, int height)
    {
        Level = level;
        Position = position;
        Width = width;
        Height = height;
        Collidable = true;
    }

    /// <summary>
    /// Moves the solid by the specified amount, handling all actor interactions.
    /// This includes pushing colliding actors and carrying riding actors.
    /// </summary>
    public void Move(float x, float y)
    {
        xRemainder += x;
        yRemainder += y;

        int moveX = (int)MathF.Round(xRemainder);
        int moveY = (int)MathF.Round(yRemainder);

        if (moveX != 0 || moveY != 0)
        {
            // Find actors to carry before movement
            FindCarriedActors();

            // Temporarily disable collisions to prevent recursive checks
            Collidable = false;

            // Handle movement one axis at a time
            if (moveX != 0)
            {
                xRemainder -= moveX;
                Position.X += moveX;
                HandleHorizontalMovement(moveX);
            }

            if (moveY != 0)
            {
                yRemainder -= moveY;
                Position.Y += moveY;
                HandleVerticalMovement(moveY);
            }

            Collidable = true;

            // Clear cached lists
            carriedActors.Clear();
            collidingActors.Clear();
        }
    }

    /// <summary>
    /// Identifies all actors that should be carried by this solid
    /// An actor is carried if it's riding (standing on top of) this solid
    /// </summary>
    private void FindCarriedActors()
    {
        carriedActors.Clear();
        foreach (var actor in Level.GetActors().ToList())
        {
            if (actor.IsRiding(this))
            {
                carriedActors.Add(actor);
            }
        }
    }

    /// <summary>
    /// Gets the collision bounds of the solid
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
    /// Handles all actor interactions during horizontal movement
    /// This includes pushing colliding actors and moving carried actors
    /// </summary>
    public void HandleHorizontalMovement(int moveX)
    {
        collidingActors.Clear();
        var bounds = GetBounds();

        // First pass: identify all colliding actors
        foreach (var actor in Level.GetActors().ToList())
        {
            if (bounds.Intersects(actor.GetBounds()))
            {
                collidingActors.Add(actor);
            }
        }

        // Second pass: handle collisions and carrying
        foreach (var actor in Level.GetActors().ToList())
        {
            if (collidingActors.Contains(actor))
            {
                // Push the actor out of the solid
                if (moveX > 0)
                {
                    actor.MoveX(Right - actor.Left, actor.Squish);
                }
                else
                {
                    actor.MoveX(Left - actor.Right, actor.Squish);
                }
            }
            else if (carriedActors.Contains(actor))
            {
                // Move carried actors along with the solid
                actor.MoveX(moveX, null);
            }
        }
    }

    /// <summary>
    /// Handles all actor interactions during vertical movement
    /// Functions similarly to HandleHorizontalMovement but for vertical movement
    /// </summary>
    private void HandleVerticalMovement(int moveY)
    {
        collidingActors.Clear();
        var bounds = GetBounds();

        // First pass: identify all colliding actors
        foreach (var actor in Level.GetActors().ToList())
        {
            if (bounds.Intersects(actor.GetBounds()))
            {
                collidingActors.Add(actor);
            }
        }

        // Second pass: handle collisions and carrying
        foreach (var actor in Level.GetActors().ToList())
        {
            if (collidingActors.Contains(actor))
            {
                // Push the actor out of the solid
                if (moveY > 0)
                {
                    actor.MoveY(Bottom - actor.Top, actor.Squish);
                }
                else
                {
                    actor.MoveY(Top - actor.Bottom, actor.Squish);
                }
            }
            else if (carriedActors.Contains(actor))
            {
                // Move carried actors along with the solid
                actor.MoveY(moveY, null);
            }
        }
    }

    /// <summary>
    /// Draws the solid's sprite if one exists
    /// </summary>
    public virtual void Draw()
    {
        if (sprite != null)
        {
            sprite.Draw(Position, sourceRect);
        }
    }
}