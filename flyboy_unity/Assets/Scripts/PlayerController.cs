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
    private float idleDrag;

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
    private bool flyUp;
    public bool flyDown;
    public float rotationSpeed;
    public float angleOfAttack;
    public float debug;
    private Vector3 upRotation;
    private Vector3 downRotation;
    private float playerDirection;
    float liftDireciton;

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

        if (debugOn) { DebugTools(); }

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
        if (jumpHeldTime > 0.1)
        {
            flying = true;
        }
        
        // Reset when player hits the ground or lets go of space
        if ((isGrounded || Input.GetKeyUp(KeyCode.Space)) && !debugFly)
        {
            // Reset the time that the player has been holding jump to 0
            jumpHeldTime = 0;
            // If we were flying then rotate the player back to an upright position
            if (flying)
            {
                rigidBody.transform.rotation = uprightRotation;
            }
            flying = false;
        }


        if (flying)
        {
            angleOfAttack = GetAngleOfAttack();
            // Linear paramters to effect lift and drag
            float liftForce = 0.0015f;
            float flyingDrag = 0.0148f;

            //after our angle of attack is so high we'll stop getting lift
            float liftFromAOA;
            if(angleOfAttack > 41)
            {
                liftFromAOA = 0;
            }
            else if(angleOfAttack < -41)
            {
                liftFromAOA = 0;
            }
            else
            {
                liftFromAOA = angleOfAttack;
            }

            liftDireciton = playerDirection - 90;
            if (liftDireciton < 0)
            {
                liftDireciton += 360;
            }
            debug = liftFromAOA;
            //Convert to how trig actually works (flip and then turn 90 degrees clockwise)
            liftDireciton -= 450;
            if (liftDireciton < 0)
            {
                liftDireciton += 360;
            }

            // Go back to radians
            liftDireciton *= Mathf.Deg2Rad;

            // Create a coeficient so x + y is always one. Then we'll apply that to the lift
            float xRad = Mathf.Cos(liftDireciton);
            float yRad = Mathf.Sin(liftDireciton);
            float xCoef = Mathf.Abs(xRad) / (Mathf.Abs(xRad) + Mathf.Abs(yRad));
            float yCoef = Mathf.Abs(yRad) / (Mathf.Abs(xRad) + Mathf.Abs(yRad));

            // Fix drag
            rigidBody.drag = flyingDrag * Mathf.Abs(angleOfAttack);

            // Our lift force is going to be based on the speed multiplied by a hardcoded lift coeficient
            float playerVelocity = Mathf.Abs(rigidBody.velocity[0]) + Mathf.Abs(rigidBody.velocity[1]);
            float lift = playerVelocity * liftFromAOA * liftForce;
            float xLift = lift * xCoef;
            float yLift = lift * yCoef;

            //Apply life force
            rigidBody.velocity = new Vector2(rigidBody.velocity.x + xLift, rigidBody.velocity.y + yLift);
            // 'W' flies down
            if (Input.GetKey(KeyCode.W) && !flyDown)
            {
                flyUp   = true;
            }
            else
            {
                flyUp = false;
            }

            // 'S' flies up
            if (Input.GetKey(KeyCode.S) && !flyUp)
            {
                flyDown = true;
            }
            else
            {
                flyDown = false;
            }

            // The way the player turns will be different depending on what direction they're facing
            if (facingRight)
            {
                upRotation = Vector3.back;
                downRotation = Vector3.forward;
            }
            else
            {
                upRotation = Vector3.forward;
                downRotation = Vector3.back;
            }

            // Pitch body if player is flying up or down
            if (flyUp) { rigidBody.transform.Rotate(upRotation * rotationSpeed * playerVelocity * Time.deltaTime); }
            if (flyDown) { rigidBody.transform.Rotate(downRotation * rotationSpeed * playerVelocity * Time.deltaTime); }

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

    float GetPlayerDirection()
    {
        // Get the direction in radians
        float dir = Mathf.Atan2(rigidBody.velocity.y, rigidBody.velocity.x);
        // Convert to degrees
        dir *= (180 / Mathf.PI);
        // Right -> Up -> Left is 0 to 180 and Right -> Down -> Left is 0 to -180
        // Lets make it like a clock where up is 0/360
        if (dir > 0)
        {
            dir = 90 - dir;
            if (dir < 0)
            {
                dir = 360 + dir;
            }
        }
        else
        {
            dir = Mathf.Abs(dir);
            dir += 90;
        }
        playerDirection = dir;
        return dir;
    }

    float GetAngleOfAttack()
    {

        // Takes in the rotation of the player. 1 is right side up and 2 is upside down.
        float aoa = (rigidBody.transform.rotation.eulerAngles.z);

        // Everything fips when we face left so lets flip it back
        if (!facingRight)
        {
            aoa -= 180;
            if (aoa < 0)
            {
                aoa += 360;
            }
        }
        
        // When we nose down the angle goes from 360 to 361 rather than 1
        if (aoa > 360)
        {
            aoa -= 360;
        }

        // Flip it and turn it so 360 is up 270 is left
        aoa = 360 - aoa;
        aoa += 90;
        if (aoa > 360)
        {
            aoa -= 360;
        }
        
        // Change it so it's in reference to the direction we're going
        float dir = GetPlayerDirection();
        
        // If 360 is 'in between' the two headings then we'll have to do some extra math.
        if (dir <=90 && aoa >= 270)
        {
            aoa = 360 + dir - aoa;
        }
        else if(dir >= 270 && aoa <= 90)
        {
            aoa = dir - 360 - aoa;
        }
        else
        {
            aoa = dir - aoa;
        }
        if (!facingRight)
        {
            aoa *= -1;
        }
        return aoa;
    }
    


    // DEBUG
    public bool debugOn;
    private bool debugFly;
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

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (!debugFly) { debugFly = true; }
            else { debugFly = false; }
        }
    }
}
