using Leteste.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Leteste.Core;
using Leteste.Levels;
using Leteste.Graphics;
using System;

namespace Leteste.Entities;

public class Player : Actor
{
    // Movement constants (in pixels per second)
    public const float MAX_SPEED = 90f;
    public const float GROUND_ACCELERATION = 1000f;
    public const float AIR_ACCELERATION = 900f;
    public const float GROUND_FRICTION = 1000f;

    // Jump and gravity constants
    public const float JUMP_FORCE = -145f;
    public const float JUMP_HORIZONTAL_BOOST = 60f;
    public const float MIN_JUMP_HEIGHT = -60f;
    public const float MAX_FALL_SPEED = 160f;
    public const float GRAVITY = 900f;
    public const float FAST_FALL_GRAVITY = 1800f;
    public const float HOLD_JUMP_GRAVITY_MULTIPLIER = 0.4f;

    // dash constants
    public const float DASH_SPEED = 240f;
    public const float DASH_TIME = 0.15f;
    public const float DASH_COOLDOWN = 0.2f;
    public const float END_DASH_SPEED = 140f;

    // Timing constants
    public const float COYOTE_TIME = 0.1f;
    public const float JUMP_BUFFER_TIME = 0.1f;
    private const float DASH_END_TIME = 0.2f;

    // Dash states
    private bool canDash = true;
    private bool isDashing;
    private bool isDashEnding;
    private float dashEndingTime;
    private float dashTimeLeft;
    private float dashCooldownLeft;
    private Vector2 dashDirection;
    private bool wasInDash;
    private bool hasReleasedDash = true;


    // Movement state
    public bool isOnGround;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool isJumpHeld;

    // Physics state
    public Vector2 Velocity;

    // Collision box configuration
    public const int HITBOX_WIDTH = 10;
    public const int HITBOX_HEIGHT = 16;

    public Player(Level level, Vector2 position)
    : base(level, position, HITBOX_WIDTH, HITBOX_HEIGHT)
    {
        sprite = new Sprite("player_static", true);
        depth = 0.5f;
    }

    public override void Update()
    {
        // First check for ground collision
        bool wasOnGround = isOnGround;
        isOnGround = CollideAt(Position + new Vector2(0, 1));

        // Update coyote time
        if (wasOnGround && !isOnGround)
        {
            coyoteTimeCounter = COYOTE_TIME;
        }
        else if (!isOnGround)
        {
            coyoteTimeCounter -= Globals.Time;
        }

        // Then handle input
        HandleHorizontalMovement();
        HandleJump();
        HandleDash();

        // Only apply normal gravity when not dashing
        if (!isDashing)
        {
            ApplyGravity();
        }

        // Finally move
        ApplyVelocity();

        base.Update();
    }

    public void HandleHorizontalMovement()
    {
        var keyboard = Keyboard.GetState();
        float targetVelocityX = 0f;

        // Determine input direction
        if (keyboard.IsKeyDown(Keys.Left))
        {
            targetVelocityX = -MAX_SPEED;
            SetFacing(false);
        }
        if (keyboard.IsKeyDown(Keys.Right))
        {
            targetVelocityX = MAX_SPEED;
            SetFacing(true);
        }

        // Apply acceleration based on ground state
        float acceleration = isOnGround ? GROUND_ACCELERATION : AIR_ACCELERATION;

        if (targetVelocityX == 0 && isOnGround)
        {
            float friction = GROUND_FRICTION * Globals.Time;
            Velocity = new Vector2(
                MathF.Abs(Velocity.X) > friction
                    ? Velocity.X - Math.Sign(Velocity.X) * friction
                    : 0,
                Velocity.Y
            );
        }
        else
        {
            // Apply acceleration
            float maxSpeedChange = acceleration * Globals.Time;
            float velocityDiff = targetVelocityX - Velocity.X;
            float actualChange = MathHelper.Clamp(velocityDiff, -maxSpeedChange, maxSpeedChange);

            Velocity = new Vector2(
                Velocity.X + actualChange,
                Velocity.Y
            );
        }
    }

    public void HandleJump()
    {
        var keyboard = Keyboard.GetState();
        bool jumpKeyPressed = keyboard.IsKeyDown(Keys.C);

        // Only buffer a new jump when the key is first pressed
        if (jumpKeyPressed && !isJumpHeld)
        {
            jumpBufferCounter = JUMP_BUFFER_TIME;
        }
        else
        {
            jumpBufferCounter -= Globals.Time;
        }

        // Process jump - only if we have both buffer and coyote time
        if (jumpBufferCounter > 0 && (isOnGround || coyoteTimeCounter > 0))
        {
            Velocity = new Vector2(
                Velocity.X + (Math.Sign(Velocity.X) * JUMP_HORIZONTAL_BOOST),
                JUMP_FORCE
            );

            // Reset jump state
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
            isOnGround = false;
        }

        // Variable jump height
        if (!jumpKeyPressed && Velocity.Y < 0)
        {
            Velocity = new Vector2(Velocity.X, Math.Max(Velocity.Y, MIN_JUMP_HEIGHT));
        }

        // Update jump hold state last
        isJumpHeld = jumpKeyPressed;
    }

    public void HandleDash()
    {
        var keyboard = Keyboard.GetState();
        bool dashKeyDown = keyboard.IsKeyDown(Keys.X);

        // Update dash state
        if (isDashing)
        {
            dashTimeLeft -= Globals.Time;
            if (dashTimeLeft <= 0)
            {
                EndDash();
            }
            else
            {
                // Maintain dash velocity
                Velocity = dashDirection * DASH_SPEED;
            }
        }

        // Update dash coldown
        if (dashCooldownLeft > 0)
        {
            dashCooldownLeft -= Globals.Time;
        }

        if (dashKeyDown && hasReleasedDash && canDash && !isDashing && dashCooldownLeft <= 0)
        {
            // Get dash direction from input
            Vector2 direction = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.Left)) direction.X -= 1;
            if (keyboard.IsKeyDown(Keys.Right)) direction.X += 1;
            if (keyboard.IsKeyDown(Keys.Up)) direction.Y -= 1;
            if (keyboard.IsKeyDown(Keys.Down)) direction.Y += 1;

            // If no direction pressed, dash horizontally based on facing
            if (direction == Vector2.Zero)
            {
                direction.X = isFacingRight ? 1 : -1;
            }

            StartDash(direction);
            hasReleasedDash = false;
        }

        // Reset dash ability when touching ground
        if (isOnGround && !isDashing && !wasInDash)
        {
            canDash = true;
        }

        if (!dashKeyDown)
        {
            hasReleasedDash = true;
        }

        wasInDash = isDashing;
    }

    public void StartDash(Vector2 direction)
    {
        isDashing = true;
        canDash = false;
        dashTimeLeft = DASH_TIME;

        // Store normalized direction
        dashDirection = Vector2.Normalize(direction);

        // Apply initial dahs velocity
        Velocity = dashDirection * DASH_SPEED;
    }

    public void EndDash()
    {
        isDashing = false;
        isDashEnding = true;
        dashTimeLeft = 0;
        dashCooldownLeft = DASH_COOLDOWN;
        dashEndingTime = DASH_END_TIME;

        // Important: Only modify velocity if we're NOT hitting a wall
        if (!CollideAt(Position + new Vector2(Math.Sign(Velocity.X), 0)))
        {
            if (Math.Abs(dashDirection.X) > Math.Abs(dashDirection.Y))
            {
                // Horizontal dash
                Velocity = new Vector2(
                    dashDirection.X * END_DASH_SPEED,
                    Velocity.Y * 0.2f
                );
            }
            else
            {
                // Vertical/Diagonal dash
                Velocity = new Vector2(
                    Velocity.X * 0.4f,
                    dashDirection.Y * END_DASH_SPEED
                );
            }
        }
    }

    public void ApplyGravity()
    {
        if (!isOnGround)
        {
            float gravityMultiplier = 1f;
            // Update dash ending state
            if (isDashEnding)
            {
                dashEndingTime -= Globals.Time;
                if (dashEndingTime <= 0)
                {
                    isDashEnding = false;
                }
                gravityMultiplier = 0.2f;  // Very low gravity during dash end
            }
            // Lower gravity when holding jump button and moving upward
            else if (Velocity.Y < 0)  // Moving upward
            {
                if (isJumpHeld)
                {
                    gravityMultiplier = HOLD_JUMP_GRAVITY_MULTIPLIER;
                }
                else
                {
                    gravityMultiplier = 0.7f;  // Medium gravity when releasing during ascent
                }
            }
            // Higher gravity when falling
            else
            {
                if (Velocity.Y < 40f)  // Just started falling
                {
                    gravityMultiplier = 0.8f;  // Slightly reduced gravity at fall start
                }
                else if (Velocity.Y > 60f)  // Falling for a while
                {
                    gravityMultiplier = FAST_FALL_GRAVITY / GRAVITY;
                }
            }


            float gravityThisFrame = GRAVITY * gravityMultiplier * Globals.Time;
            Velocity = new Vector2(
                Velocity.X,
                MathHelper.Clamp(
                    Velocity.Y + gravityThisFrame,
                    -MAX_FALL_SPEED,
                    MAX_FALL_SPEED
                )
            );
        }
    }

    public void ApplyVelocity()
    {
        if (Velocity != Vector2.Zero)
        {
            MoveX(Velocity.X * Globals.Time, OnCollideX);
            MoveY(Velocity.Y * Globals.Time, OnCollideY);
        }
    }

    public void OnCollideX()
    {
        if (isDashing)
        {
            // Only convert diagonal dashes with upward component
            if (dashDirection.Y < 0)  // Changed from <= 0 to < 0
            {
                // Convert to upward dash, preserving the original upward momentum
                dashDirection = new Vector2(0, dashDirection.Y);  // Keep original Y component
                Velocity = dashDirection * DASH_SPEED;
                return;  // Don't end the dash yet
            }
            EndDash();
        }
        else
        {
            Velocity = new Vector2(0, Velocity.Y);
        }
    }

    public void OnCollideY()
    {
        if (isDashing)
        {
            EndDash();
        }
        if (Velocity.Y > 0)
        {
            isOnGround = true;
        }
        Velocity = new Vector2(Velocity.X, 0);
    }

    // Overrides the collision bounds of the actor
    public override Rectangle GetBounds()
    {
        return new Rectangle(
            (int)Position.X - HITBOX_WIDTH / 2,
            (int)Position.Y - HITBOX_HEIGHT / 2,
            HITBOX_WIDTH,
            HITBOX_HEIGHT
        );
    }
}