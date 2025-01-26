using System;
using System.Collections.Generic;
using System.Linq;
using Leteste.Graphics;
using Leteste.Levels;
using Microsoft.Xna.Framework;

namespace Leteste.Physics;

// Represents a solid object in the game world that other entities can collide with.
// Can be static (like walls) or dynamic (like moving platforms).
public class Solid
{
    // Core properties
    public Vector2 Position;
    public bool Collidable;
    public int Width;
    public int Height;
    public Level Level;
    public bool IsOneWay;

    // Rendering components
    public Sprite sprite;
    public Rectangle? sourceRect;

    // Physics state
    public float yRemainder;
    public float xRemainder;

    // Cached lists for performance
    public List<Actor> carriedActors = new();
    public List<Actor> collidingActors = new();

    // Collision box properties
    public int Left => GetBounds().Left;
    public int Right => GetBounds().Right;
    public int Top => GetBounds().Top;
    public int Bottom => GetBounds().Bottom;

    public Solid(Level level, Vector2 position, int width, int height)
    {
        Level = level;
        Position = position;
        Width = width;
        Height = height;
        Collidable = true;
    }

    // Moves the solid by the specified amount, handling all actor interactions.
    public void Move(float x, float y)
    {
        xRemainder += x;
        yRemainder += y;

        int moveX = (int)MathF.Round(xRemainder);
        int moveY = (int)MathF.Round(yRemainder);

        if (moveX != 0 || moveY != 0)
        {
            FindCarriedActors();

            Collidable = false;

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

            carriedActors.Clear();
            collidingActors.Clear();
        }
    }

    // Finds all actors that should be carried by this solid before movement
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

    // Gets the collision bounds of the solid
    public virtual Rectangle GetBounds()
    {
        return new Rectangle(
            (int)Position.X,
            (int)Position.Y,
            Width,
            Height
        );
    }

    // Handles all actor interactions during horizontal movement
    public void HandleHorizontalMovement(int moveX)
    {
        collidingActors.Clear();
        var bounds = GetBounds();

        // First pass: find all colliding actors
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
                // Push the actor in the movement direction
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
                // Carry the actor along
                actor.MoveX(moveX, null);
            }
        }
    }

    // Same as HandleHorizontalMovement but vertically
    private void HandleVerticalMovement(int moveY)
    {
        collidingActors.Clear();
        var bounds = GetBounds();

        foreach (var actor in Level.GetActors().ToList())
        {
            if (bounds.Intersects(actor.GetBounds()))
            {
                collidingActors.Add(actor);
            }
        }

        foreach (var actor in Level.GetActors().ToList())
        {
            if (collidingActors.Contains(actor))
            {
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
                actor.MoveY(moveY, null);
            }
        }
    }

    public virtual void Draw()
    {
        if (sprite != null)
        {
            sprite.Draw(Position, sourceRect);
        }
    }
}