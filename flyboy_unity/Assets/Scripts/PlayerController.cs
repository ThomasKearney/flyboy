using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // BASIC
    private float speedInput = 10;
    public Animator animator;
    private float distToGround;
    private float speed;
    public CapsuleCollider2D playerCollider;
    private float moveInput;
    private float idleDrag = 0;
    private float flyingDrag = 0;

    private Quaternion uprightRotation;

    // JUMPING
    float fallMultiplier = 2.5f;
    float lowJumpMultiplier = 2f;
    private float jumpForce = 5;

    private Rigidbody2D rigidBody;

    // FLIP
    private bool facingRight = true;

    // GROUNDING
    private bool isGrounded;
    public Transform groundCheck;
    public float checkRadius;
    public LayerMask whatIsGround;

    // SOFT FALL
    private float softFallDrag = 5;
    private float softFallHorizontalDrag = 5;

    // FLYING
    private float jumpHeldTime;
    private bool jumping;
    private bool flying;
    private float liftForce = 2;
    private bool flyUp;
    public bool flyDown;
    public float rotationSpeed;
    public float angleOfAttack;
    private Vector3 upRotation;
    private Vector3 downRotation;

    private Vector3 defaultPostition;

    private void Start()
    {
        speed = speedInput;
        rigidBody = GetComponent<Rigidbody2D>();
        distToGround = playerCollider.bounds.extents.y;
        defaultPostition = rigidBody.transform.position;
        uprightRotation = rigidBody.transform.rotation;
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
        if (rigidBody.velocity.y < 0 && !isGrounded && (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Space)))
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
            if (isGrounded) { rigidBody.velocity = Vector2.up * jumpForce; }
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

        // If we hold jump then start flying state
        if (jumpHeldTime > 0.1) { flying = true; }

        // Reset when player hits the ground or lets go of space
        if (isGrounded || Input.GetKeyUp(KeyCode.Space))
        {
            // Reset the time that the player has been holding jump to 0
            jumpHeldTime = 0;
            // If we were flying then rotate the player back to an upright position
            if (flying) { rigidBody.transform.rotation = uprightRotation; }
            flying = false;
        }


        if (flying)
        {
            // Fix drag
            rigidBody.drag = flyingDrag;
            // Our lift force is going to be based on the horizontal speed multiplied by a hardcoded lift coeficient
            float horizontalSpeed = rigidBody.velocity.x;
            horizontalSpeed = Mathf.Abs(horizontalSpeed);
            // Takes in the rotation of the player. 1 is right side up and 2 is upside down.
            angleOfAttack = 1 + Mathf.Abs(rigidBody.transform.rotation.z);
            float lift = (horizontalSpeed * liftForce * angleOfAttack) / 100;

            // Apply life force
            rigidBody.velocity = new Vector2(rigidBody.velocity.x, rigidBody.velocity.y + lift);

            if (Input.GetKey(KeyCode.W)) { flyUp = true; }
            if (Input.GetKeyUp(KeyCode.W)) { flyUp = false; }
            if (Input.GetKey(KeyCode.S)) { flyDown = true; }
            if (Input.GetKeyUp(KeyCode.S)) { flyDown = false; }

            // The way the player turns will be different depending on what direction they're facing
            if (facingRight) { upRotation = Vector3.forward; } else { upRotation = Vector3.back; }
            if (!facingRight) { downRotation = Vector3.back; } else { downRotation = Vector3.forward; }

            // Pitch body if player is flying up or down
            if (flyUp) { rigidBody.transform.Rotate(upRotation * rotationSpeed * rigidBody.velocity * Time.deltaTime); }
            if (flyDown) { rigidBody.transform.Rotate(downRotation * rotationSpeed * rigidBody.velocity * Time.deltaTime); }

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
        if (isGrounded)
        {
            rigidBody.drag = idleDrag;
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
            rigidBody.transform.rotation = uprightRotation;
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
