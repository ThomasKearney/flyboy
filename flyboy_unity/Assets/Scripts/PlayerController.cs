using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // BASIC
    public float speedInput;
    public Animator animator;
    private float distToGround;
    public float speed;
    public float jumpForce;
    public CapsuleCollider2D playerCollider;
    private float moveInput;

    private Rigidbody2D rigidBody;

    // FLIP
    private bool facingRight = true;

    // GROUNDING
    public bool isGrounded;
    public Transform groundCheck;
    public float checkRadius;
    public LayerMask whatIsGround;

    // SOFT FALL
    public float softFallDrag;
    public float softFallHorizontalDrag;

    // FLYING
    private float jumpHeldTime;
    private bool jumping;
    private bool flying;
    public float liftForce;

    private void Start()
    {
        speed = speedInput;
        rigidBody = GetComponent<Rigidbody2D>();
        distToGround = playerCollider.bounds.extents.y;
    }

    private void Update()
    {
        JumpController();
    }

    private void FixedUpdate()
    {
        // Check if the player is on the ground
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        //SoftLandController();

        // Right arrow =  1
        // Left arrow  = -1
        moveInput = Input.GetAxis("Horizontal");

        WalkController();

        AnimationController();

        FlyController();

    }

    private void SoftLandController()
    {
        // If we are falling, not on the ground, and are holding a jump button then increase our drag
        if(rigidBody.velocity.y < 0 && !isGrounded && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space)))
        {
            rigidBody.drag = softFallDrag;
            speed = speedInput / softFallHorizontalDrag;
            animator.SetBool("slowFall", true);
        }
        else
        {
            rigidBody.drag = 0;
            speed = speedInput;
            animator.SetBool("slowFall", false);
        }

    }

    private void JumpController()
    {
        // Jump if spacebar is hit
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Add force if we're on the ground
            if (isGrounded){ rigidBody.velocity = Vector2.up * jumpForce; }
            jumping = true;
        }

        // Set our jumping state to false when spacebar is let go
        if (Input.GetKeyUp(KeyCode.Space)) 
        { 
            jumping = false;
        }


    }

    private void FlyController()
    {
        // Start a timer to see how long we've been jumping for
        if (jumping) { jumpHeldTime += Time.deltaTime; }

        // If we jump for over half a second then start flying state
        if (jumpHeldTime > 0.5) { flying = true; }

        // Reset when player hits the ground or lets go of space
        if (isGrounded || Input.GetKeyUp(KeyCode.Space))
        {
            jumpHeldTime = 0;
            flying = false;
        }

        
        if(flying)
        {
            // Our lift force is going to be based on the horizontal speed multiplied by a hardcoded lift coeficient
            float horizontalSpeed = rigidBody.velocity.x;
            horizontalSpeed = Mathf.Abs(horizontalSpeed);
            float lift = horizontalSpeed * liftForce;

            // Apply life force
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, rigidBody.velocity.y + lift);
        }

    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 playerLocalScaler = transform.localScale;
        playerLocalScaler.x *= -1;
        transform.localScale = playerLocalScaler;
    }

    void AnimationController()
    {
        // Walk
        animator.SetBool("onGround", isGrounded);
        if (moveInput > 0 || moveInput < 0)
        {
            animator.SetBool("moving", true);
        }
        else { animator.SetBool("moving", false); }

        // Fall
        if (rigidBody.velocity.y < -0.1)
        { 
            animator.SetBool("falling", true);
        }
        else { animator.SetBool("falling", false); }

        if (flying) { animator.SetBool("flying", true); }
        else { animator.SetBool("flying", false); }
    }

    void WalkController()
    {
        // Multiply a hardcoded speed by move input and set the horizontal speed to it
        // If we're flying we don't want to apply this force
        if (!flying)
        {
            rigidBody.velocity = new Vector2(moveInput * speed, rigidBody.velocity.y);

            // Turn player in the right direction
            if (facingRight == false && moveInput > 0) { Flip(); }
            else if (facingRight == true && moveInput < 0) { Flip(); }
        }
    }

}
