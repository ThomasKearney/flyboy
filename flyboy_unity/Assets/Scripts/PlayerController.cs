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
    public CapsuleCollider2D playerCollider;
    private float moveInput;

    // JUMPING
    float fallMultiplier = 2.5f;
    float lowJumpMultiplier = 2f;
    public float jumpForce;

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
    private bool flyUp;
    private bool flyDown;
    public float rotationSpeed;

    private Vector3 defaultPostition;

    private void Start()
    {
        speed = speedInput;
        rigidBody = GetComponent<Rigidbody2D>();
        distToGround = playerCollider.bounds.extents.y;
        defaultPostition = rigidBody.transform.position;   
    }

    private void Update()
    {
        JumpController();
    }

    private void FixedUpdate()
    {
        CheckIfGrounded();

        //SoftLandController();

        // Right arrow =  1
        // Left arrow  = -1
        moveInput = Input.GetAxis("Horizontal");

        WalkController();

        AnimationController();

        FlyController();

        DebugTools();

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


        //faster falling
        if (!flying)
        {
            if (rigidBody.velocity.y < 0)
            {
                rigidBody.velocity += Vector2.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
            }

            //control jump height by length of time jump button held
            if (rigidBody.velocity.y > 0 && !Input.GetKeyDown(KeyCode.Space))
            {
                rigidBody.velocity += Vector2.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
            }
        }
        




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
        if (jumpHeldTime > 0.45) { flying = true; }

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
            float lift = (horizontalSpeed * liftForce) / 100;

            // Apply life force
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, rigidBody.velocity.y + lift);

            if (Input.GetKeyDown(KeyCode.W)) { flyUp   = true; }
            if (Input.GetKeyUp(KeyCode.W))   { flyUp   = false; }
            if (Input.GetKeyDown(KeyCode.S)) { flyDown = true; }
            if (Input.GetKeyUp(KeyCode.S))   { flyDown = false; }
        }
        else
        {
            flyDown = flyUp = false;
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

        animator.SetBool("flying", flying);
        animator.SetBool("flyUp", flyUp);
        animator.SetBool("flyDown", flyDown);

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

    void CheckIfGrounded()
    {
        Vector2 leftCorner = new Vector2(transform.position.x - 0.5f, transform.position.y - 0.5f);
        Vector2 rightCorner = new Vector2(transform.position.x + 0.5f, transform.position.y - 0.73f);
        isGrounded = Physics2D.OverlapArea(leftCorner, rightCorner, whatIsGround);
    }

    public bool rotate;
    void DebugTools()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Send player back to default position
            rigidBody.transform.position = defaultPostition;
            flying = false;
            rigidBody.velocity = new Vector2(0,0);
        }

        if (Input.GetKeyDown(KeyCode.K)) { rotate = true;   }
        if (Input.GetKeyUp(KeyCode.K)) { rotate = false;  }

        if (rotate)
        {
            // Rotate player (hopefully)
            rigidBody.transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        }
    }
}
