using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine.XR;

public class PlayerController : MonoBehaviour
{
    /* Jumping: */
    public float jumpSpeed;
    public float bunnyHopJumpSpeedIncrease = 1.2f;
    public float maxJumpSpeed = 15f;
    public float jumpForce = 5f;
    public float gravity = -9.81f;
    public float bunnyHopTimeWindow = 0.2f;
    public float doubleJumpRotationDuration = 0.5f;
    public float jumpBodyAngle = 25f;
    private float prevY;
    private float timeSinceLanding;
    private bool isJumping;
    private bool doubleJumped;
    
    /* Dashing: */
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashRotationAngle = 45f;
    private bool isDashing;

    /* Walking: */
    public float moveSpeed = 5f;
    
    /* Components: */
    private Rigidbody2D rb2d;
    private Vector2 movement;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        jumpSpeed = rb2d.velocity.x;
    }

    private void Update()
    {
        HandleKeyboardInput();
        MeasureTimeSinceLanding();
    }

    private void FixedUpdate()
    {
        StandardMovementPhysics();
        JumpingPhysics();
        RestartPhysicsIfLanded();
    }

    /* Check if the player has landed on the collider. Disable jump so player can move again. */
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Collider"))
        {
            return;
        }
        isJumping = false;
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
    
    /* Move up, down, left, right, dash, jump, double jump, bunny hop. */
    private void HandleKeyboardInput()
    {
        HandleDoubleJump();
        HandleWalking();
        HandleJump();
        HandleDash();
    }
    
    /* Measure time to know if bunny hop possible. */
    private void MeasureTimeSinceLanding()
    {
        if (!isJumping)
        {
            timeSinceLanding += Time.deltaTime;
        }
    }
    
    /* If during this jump player not double jumped already, perform double jump and mark it as doubleJumped so player
     cannot do it again during this jump. */
    private void HandleDoubleJump()
    {
        if (!isJumping || doubleJumped || !Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }
        doubleJumped = true;
        rb2d.velocity = new Vector2(moveSpeed, jumpForce);
        StartCoroutine(DoubleJumpRotation());
    }
    
    /* Rotate the body while double jumping. */
    private IEnumerator DoubleJumpRotation()
    {
        var elapsedTime = 0f;
        var initialZRotation = transform.eulerAngles.z;
        var rotationDirection = spriteRenderer.flipX ? -360f : 360f;
        var targetZRotation = initialZRotation + rotationDirection;
        while (elapsedTime < doubleJumpRotationDuration)
        {
            var currentZRotation = 
                Mathf.Lerp(initialZRotation, targetZRotation, elapsedTime / doubleJumpRotationDuration);
            transform.rotation = Quaternion.Euler(0, 0, currentZRotation);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0, 0, targetZRotation);
    }
    
    /* Handle walking input and sprite direction. */
    private void HandleWalking()
    {
        if (isJumping)
        {
            return;
        }
        movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        spriteRenderer.flipX = movement.x switch
        {
            > 0 => true,
            < 0 => false,
            _ => spriteRenderer.flipX
        };
    }

    /* Handle initial jump logic. */
    private void HandleJump()
    {
        if (isJumping || !Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }
        doubleJumped = false;
        prevY = rb2d.position.y;
        jumpSpeed = timeSinceLanding <= bunnyHopTimeWindow ?
            Mathf.Min(jumpSpeed * bunnyHopJumpSpeedIncrease, maxJumpSpeed) : moveSpeed;
        if ((jumpSpeed > 0 && movement.x < 0) || (jumpSpeed < 0 && movement.x > 0))
        {
            jumpSpeed *= -1;
        }
        timeSinceLanding = 0f;
        rb2d.velocity = new Vector2(jumpSpeed, jumpForce);
        isJumping = true;
        var rotationAngle = spriteRenderer.flipX ? jumpBodyAngle : -jumpBodyAngle;
        transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
    }
    
    /* Control when dash can be performed and if possible perform it. */
    private void HandleDash()
    {
        if (isJumping || isDashing || !Input.GetKeyDown(KeyCode.LeftShift))
        {
            return;
        }
        StartCoroutine(Dash());
    }
    
    /* Perform dash with rotation. */
    private IEnumerator Dash()
    {
        isDashing = true;
        rb2d.velocity = movement.normalized * dashSpeed;
        var rotationAngle = spriteRenderer.flipX ? -dashRotationAngle : dashRotationAngle;
        transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
        yield return new WaitForSeconds(dashDuration);
        transform.rotation = Quaternion.Euler(0, 0, 0);
        isDashing = false;
    }

    /* Handle standard movement physics when not jumping or dashing. */
    private void StandardMovementPhysics()
    {
        if (isDashing || isJumping)
        {
            return;
        }
        rb2d.velocity = new Vector2(movement.x * moveSpeed, movement.y * moveSpeed);
    }

    /* Apply gravity when jumping. */
    private void JumpingPhysics()
    {
        if (isDashing || !isJumping)
        {
            return;
        }
        var verticalForce = movement.x == 0 ? 0 : jumpSpeed; 
        rb2d.velocity = new Vector2(verticalForce, rb2d.velocity.y + (gravity * Time.fixedDeltaTime));
    }
    
    /* Check if the player has landed and reset physics. */
    private void RestartPhysicsIfLanded()
    {
        if (isDashing || !isJumping || !(rb2d.velocity.y <= 0) || !(rb2d.position.y <= prevY))
        {
            return;
        }
        isJumping = false;
        rb2d.velocity = new Vector2(rb2d.velocity.x, 0); // Stop downward movement.
        rb2d.position = new Vector2(rb2d.position.x, prevY); // Snap to initial jump position.
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
}