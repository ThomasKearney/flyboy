using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // BASIC
    public float speedInput;
    public Animator animator;
    //
    public float speed;
    public float jumpForce;
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
    }

    private void Update()
    {
        JumpController();
    }

    private void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, whatIsGround);

        //SoftLandController();

        // Right arrow =  1
        // Left arrow  = -1
        moveInput = Input.GetAxis("Horizontal");

        rigidBody.velocity = new Vector2(moveInput * speed, rigidBody.velocity.y);

        if(facingRight == false && moveInput > 0)
        {
            Flip();
        } else if(facingRight == true && moveInput < 0) {
            Flip();
        }

        AnimationController(moveInput, isGrounded);
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
        if (Input.GetKeyDown(KeyCode.Space))
        {   
            if (isGrounded){ rigidBody.velocity = Vector2.up * jumpForce; }
            jumping = true;
        }

        if (Input.GetKeyUp(KeyCode.Space)) 
        { 
            jumping = false;
        }


    }

    private void FlyController()
    {

        if (jumping) { jumpHeldTime += Time.deltaTime; }

        if (jumpHeldTime > 0.5) { flying = true; }

        if (isGrounded)
        {
            jumpHeldTime = 0;
            flying = false;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpHeldTime = 0;
            flying = false;
        }

        if(flying)
        {
            float hSpeed = rigidBody.velocity.x;
            hSpeed = Mathf.Abs(hSpeed);
            float lift = hSpeed * liftForce;
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

    void AnimationController(float movement, bool grounded)
    {
        // Walk
        animator.SetBool("onGround", grounded);
        if (movement > 0 ||  movement < 0)
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

}
