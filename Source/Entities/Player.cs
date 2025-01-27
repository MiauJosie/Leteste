using Leteste.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Leteste.Core;
using Leteste.Levels;
using Leteste.Graphics;
using System;

namespace Leteste.Entities;

/// <summary>
/// Player class handling all player movement, physics, and abilities
/// </summary>
public class Player : Actor
{
    // Movement constants
    public const float MAX_SPEED = 90f;
    public const float GROUND_ACCELERATION = 1000f;
    public const float AIR_ACCELERATION = 900f;       // Slower acceleration while airborne
    public const float GROUND_FRICTION = 1000f;       // Deceleration when not moving horizontally

    // Jump-related constants
    public const float JUMP_FORCE = -145f;            // Negative value for upward movement
    public const float JUMP_HORIZONTAL_BOOST = 60f;   // Extra horizontal speed boost when jumping
    public const float MIN_JUMP_HEIGHT = -60f;        // Minimum jump height when button is released early
    public const float MAX_FALL_SPEED = 160f;         // Terminal velocity when falling
    public const float GRAVITY = 900f;                // Base gravity value
    public const float FAST_FALL_GRAVITY = 1800f;     // Increased gravity when falling fast
    public const float HOLD_JUMP_GRAVITY_MULTIPLIER = 0.4f;  // Reduced gravity when holding jump button

    // Dash-related constants
    public const float DASH_SPEED = 240f;             // Speed during dash
    public const float DASH_TIME = 0.15f;             // Duration of the dash
    public const float DASH_COOLDOWN = 0.2f;          // Time before player can dash again
    public const float END_DASH_SPEED = 140f;         // Speed maintained after dash ends

    // Climbing constants
    public const float CLIMB_MAX_SPEED = 45f;         // Vertical climbing speed
    public const float CLIMB_ACCELERATION = 900f;     // How fast you accelerate while climbing
    public const float CLIMB_JUMP_FORCE = -130f;      // Wall jump force
    public const float CLIMB_JUMP_H_BOOST = 120f;     // Horizontal boost when wall jumping
    public const float CLIMB_SLIDE_SPEED = 40f;       // Speed of wall sliding
    public const float MAX_CLIMB_STAMINA = 110f;      // About 2 seconds of climb time
    public const float CLIMB_STAMINA_DRAIN = 45f;     // Stamina drain per second
    public const float CLIMB_STAMINA_GAIN = 25f;      // Stamina gain per second when grounded

    // Movement assistance timers
    public const float COYOTE_TIME = 0.1f;            // Time window to jump after leaving platform
    public const float JUMP_BUFFER_TIME = 0.1f;       // Time window to queue up a jump before landing
    private const float DASH_END_TIME = 0.2f;         // Duration of dash ending state

    // Climbing state tracking
    private bool isGrabbing;
    private bool canGrab = true;
    private float stamina = MAX_CLIMB_STAMINA;
    private int grabWallDir;  // -1 for left wall, 1 for right wall

    // Dash state tracking
    private bool canDash = true;                      // Whether the player can currently dash
    private bool isDashing;                           // Currently in dash state
    private bool isDashEnding;                        // In dash ending state
    private float dashEndingTime;                     // Time remaining in dash ending state
    private float dashTimeLeft;                       // Time remaining in current dash
    private float dashCooldownLeft;                   // Cooldown timer before next dash
    private Vector2 dashDirection;                    // Current dash direction vector
    private bool wasInDash;                           // Was dashing in previous frame
    private bool hasReleasedDash = true;              // Has released dash button since last dash

    // Movement state tracking
    public bool isOnGround;                           // Currently touching ground
    private float coyoteTimeCounter;                  // Time left for coyote time
    private float jumpBufferCounter;                  // Time left in jump buffer
    private bool isJumpHeld;                          // Jump button is being held

    // Physics
    public Vector2 Velocity;                          // Current movement velocity

    // Collision box dimensions
    public const int HITBOX_WIDTH = 10;
    public const int HITBOX_HEIGHT = 16;

    /// <summary>
    /// Initialize the player with position and hitbox
    /// </summary>
    public Player(Level level, Vector2 position)
    : base(level, position, HITBOX_WIDTH, HITBOX_HEIGHT)
    {
        sprite = new Sprite("player_static", true);
        depth = 0.5f;
    }

    /// <summary>
    /// Main update loop for player logic
    /// </summary>
    public override void Update()
    {
        // Check ground state and update coyote time
        bool wasOnGround = isOnGround;
        isOnGround = CollideAt(Position + new Vector2(0, 1));

        if (wasOnGround && !isOnGround)
        {
            coyoteTimeCounter = COYOTE_TIME;
        }
        else if (!isOnGround)
        {
            coyoteTimeCounter -= Globals.Time;
        }

        // Process movement and abilities
        HandleHorizontalMovement();
        HandleJump();
        HandleDash();
        HandleClimbing();

        // Apply gravity if not dashing or grabbing
        if (!isDashing && !isGrabbing)
        {
            ApplyGravity();
        }

        // Recover stamina when grounded
        if (isOnGround)
        {
            stamina = Math.Min(MAX_CLIMB_STAMINA, stamina + CLIMB_STAMINA_GAIN * Globals.Time);
            canGrab = true;
        }

        ApplyVelocity();

        base.Update();
    }

    /// <summary>
    /// Handles wall grabbing and climbing mechanics
    /// </summary>
    public void HandleClimbing()
    {
        var keyboard = Keyboard.GetState();
        bool grabKey = keyboard.IsKeyDown(Keys.Z);

        // Check for wall grab
        if (!isOnGround && grabKey && canGrab && !isDashing)
        {
            // Check both left and right walls
            for (int dir = -1; dir <= 1; dir += 2)
            {
                if (CollideAt(Position + new Vector2(dir, 0)))
                {
                    isGrabbing = true;
                    grabWallDir = dir;
                    break;
                }
            }
        }

        // Release grab if key released or out of stamina
        if (!grabKey || stamina <= 0)
        {
            isGrabbing = false;
        }

        // Handle climbing movement
        if (isGrabbing)
        {
            // Vertical climbing movement
            float targetVelocityY = 0;
            if (keyboard.IsKeyDown(Keys.Up)) targetVelocityY = -CLIMB_MAX_SPEED;
            if (keyboard.IsKeyDown(Keys.Down)) targetVelocityY = CLIMB_MAX_SPEED;

            // Apply climb acceleration
            float maxSpeedChange = CLIMB_ACCELERATION * Globals.Time;
            float velocityDiff = targetVelocityY - Velocity.Y;
            Velocity = new Vector2(
                0,
                Velocity.Y + MathHelper.Clamp(velocityDiff, -maxSpeedChange, maxSpeedChange)
            );

            // Drain stamina
            stamina = Math.Max(0, stamina - CLIMB_STAMINA_DRAIN * Globals.Time);
        }
        // Apply wall slide when touching wall but not grabbing
        else if (!isOnGround && !isDashing && !isDashEnding)
        {
            bool holdingLeft = keyboard.IsKeyDown(Keys.Left);
            bool holdingRight = keyboard.IsKeyDown(Keys.Right);

            // Check both walls
            for (int dir = -1; dir <= 1; dir += 2)
            {
                if (CollideAt(Position + new Vector2(dir, 0)))
                {
                    // Only slide if holding direction toward wall
                    if ((dir < 0 && holdingLeft) || (dir > 0 && holdingRight))
                    {
                        Velocity = new Vector2(
                            Velocity.X,
                            Math.Min(Velocity.Y, CLIMB_SLIDE_SPEED)
                        );
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Handles horizontal movement including acceleration and friction
    /// </summary>
    public void HandleHorizontalMovement()
    {
        var keyboard = Keyboard.GetState();
        float targetVelocityX = 0f;

        // Determine movement direction from input
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

        // Use different acceleration values for ground and air
        float acceleration = isOnGround ? GROUND_ACCELERATION : AIR_ACCELERATION;

        if (targetVelocityX == 0 && isOnGround)
        {
            // Apply friction when not moving horizontally on ground
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
            // Smoothly accelerate towards target velocity
            float maxSpeedChange = acceleration * Globals.Time;
            float velocityDiff = targetVelocityX - Velocity.X;
            float actualChange = MathHelper.Clamp(velocityDiff, -maxSpeedChange, maxSpeedChange);

            Velocity = new Vector2(
                Velocity.X + actualChange,
                Velocity.Y
            );
        }
    }

    /// <summary>
    /// Handles jump input, jump buffering, and variable jump height
    /// </summary>
    public void HandleJump()
    {
        var keyboard = Keyboard.GetState();
        bool jumpKeyPressed = keyboard.IsKeyDown(Keys.C);

        // Wall jump
        if (jumpKeyPressed && !isJumpHeld && isGrabbing)
        {
            Velocity = new Vector2(
                -grabWallDir * CLIMB_JUMP_H_BOOST,
                CLIMB_JUMP_FORCE
            );
            isGrabbing = false;
            canGrab = false;  // Prevent re-grabbing immediately
            jumpBufferCounter = 0;
            return;
        }

        // Handle jump buffer timing
        if (jumpKeyPressed && !isJumpHeld)
        {
            jumpBufferCounter = JUMP_BUFFER_TIME;
        }
        else
        {
            jumpBufferCounter -= Globals.Time;
        }

        // Execute jump if buffered and able (on ground or in coyote time)
        if (jumpBufferCounter > 0 && (isOnGround || coyoteTimeCounter > 0))
        {
            Velocity = new Vector2(
                Velocity.X + (Math.Sign(Velocity.X) * JUMP_HORIZONTAL_BOOST),
                JUMP_FORCE
            );

            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
            isOnGround = false;
        }

        // Variable jump height when releasing jump button
        if (!jumpKeyPressed && Velocity.Y < 0)
        {
            Velocity = new Vector2(Velocity.X, Math.Max(Velocity.Y, MIN_JUMP_HEIGHT));
        }

        isJumpHeld = jumpKeyPressed;
    }

    /// <summary>
    /// Handles dash ability input and execution
    /// </summary>
    public void HandleDash()
    {
        var keyboard = Keyboard.GetState();
        bool dashKeyDown = keyboard.IsKeyDown(Keys.X);

        // Update current dash
        if (isDashing)
        {
            dashTimeLeft -= Globals.Time;
            if (dashTimeLeft <= 0)
            {
                EndDash();
            }
            else
            {
                Velocity = dashDirection * DASH_SPEED;
            }
        }

        // Update dash cooldown
        if (dashCooldownLeft > 0)
        {
            dashCooldownLeft -= Globals.Time;
        }

        // Start new dash if conditions are met
        if (dashKeyDown && hasReleasedDash && canDash && !isDashing && dashCooldownLeft <= 0)
        {
            // Determine dash direction from input
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

    /// <summary>
    /// Initiates a dash in the specified direction
    /// </summary>
    public void StartDash(Vector2 direction)
    {
        isDashing = true;
        canDash = false;
        dashTimeLeft = DASH_TIME;
        dashDirection = Vector2.Normalize(direction);
        Velocity = dashDirection * DASH_SPEED;
    }

    /// <summary>
    /// Ends the current dash and applies end-dash momentum
    /// </summary>
    public void EndDash()
    {
        isDashing = false;
        isDashEnding = true;
        dashTimeLeft = 0;
        dashCooldownLeft = DASH_COOLDOWN;
        dashEndingTime = DASH_END_TIME;

        // Only modify velocity if not colliding with a wall
        if (!CollideAt(Position + new Vector2(Math.Sign(Velocity.X), 0)))
        {
            if (Math.Abs(dashDirection.X) > Math.Abs(dashDirection.Y))
            {
                // Horizontal dash: maintain horizontal momentum
                Velocity = new Vector2(
                    dashDirection.X * END_DASH_SPEED,
                    Velocity.Y * 0.2f
                );
            }
            else
            {
                // Vertical/Diagonal dash: maintain vertical momentum
                Velocity = new Vector2(
                    Velocity.X * 0.4f,
                    dashDirection.Y * END_DASH_SPEED
                );
            }
        }
    }

    /// <summary>
    /// Applies variable gravity based on current state
    /// </summary>
    public void ApplyGravity()
    {
        if (!isOnGround)
        {
            float gravityMultiplier = 1f;

            // Different gravity states for smoother feel:
            // 1. Very low gravity during dash end
            if (isDashEnding)
            {
                dashEndingTime -= Globals.Time;
                if (dashEndingTime <= 0)
                {
                    isDashEnding = false;
                }
                gravityMultiplier = 0.2f;
            }
            // 2. Lower gravity when holding jump and moving up
            else if (Velocity.Y < 0)
            {
                gravityMultiplier = isJumpHeld ? HOLD_JUMP_GRAVITY_MULTIPLIER : 0.7f;
            }
            // 3. Variable gravity when falling for better game feel
            else
            {
                if (Velocity.Y < 40f)
                {
                    gravityMultiplier = 0.8f;  // Smoother transition to falling
                }
                else if (Velocity.Y > 60f)
                {
                    gravityMultiplier = FAST_FALL_GRAVITY / GRAVITY;  // Fast fall
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

    /// <summary>
    /// Applies current velocity to position with collision checking
    /// </summary>
    public void ApplyVelocity()
    {
        if (Velocity != Vector2.Zero)
        {
            MoveX(Velocity.X * Globals.Time, OnCollideX);
            MoveY(Velocity.Y * Globals.Time, OnCollideY);
        }
    }

    /// <summary>
    /// Handles horizontal collision response
    /// </summary>
    public void OnCollideX()
    {
        if (isDashing)
        {
            // Convert horizontal collision during upward diagonal dash into vertical dash
            if (dashDirection.Y < 0)
            {
                dashDirection = new Vector2(0, dashDirection.Y);
                Velocity = dashDirection * DASH_SPEED;
                return;
            }
            EndDash();
        }
        else
        {
            Velocity = new Vector2(0, Velocity.Y);
        }
    }

    /// <summary>
    /// Handles vertical collision response
    /// </summary>
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

    /// <summary>
    /// Returns the collision bounds of the player
    /// </summary>
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