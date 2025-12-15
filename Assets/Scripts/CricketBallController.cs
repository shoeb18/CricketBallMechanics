using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CricketBallController : MonoBehaviour
{
    [Header("Delivery Settings")]
    public float deliverySpeed = 25f; 
    
    [Header("Physics Tuning")]
    [Tooltip("Curve strength. Default: 2.0")]
    public float swingStrength = 2.0f; 
    [Tooltip("Spin strength. Default: 4.0")]
    public float spinStrength = 4.0f;  

    [Header("Bounce Settings")]
    [Range(0.1f, 1f)]
    public float bounciness = 0.5f; 
    [Range(0f, 1f)]
    public float gripLoss = 0.15f;  

    // Internal State
    private Rigidbody rb;
    private int ballType = 1; 
    private float accuracyModifier = 1f;
    private bool inAir = false;
    private bool hasBounced = false;
    private bool isBowlingRightSide = true; // Stores the side

    private Vector3 lastFrameVelocity; 
    private TrailRenderer trail; 

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();
        
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        if(GetComponent<Collider>())
            GetComponent<Collider>().material = null; 
    }

    void HardResetPhysics()
    {
        rb.isKinematic = true; 
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        lastFrameVelocity = Vector3.zero;
        rb.isKinematic = false; 
        rb.WakeUp(); 
    }

    void FixedUpdate()
    {
        if (!inAir) return;

        lastFrameVelocity = rb.linearVelocity;

        // === SWING LOGIC (AIR ONLY) ===
        if (!hasBounced && ballType == 1) 
        {
            float force = swingStrength * accuracyModifier; 
            
            // SIDE LOGIC:
            // Right Side Bowling -> Swing Right to Left (Vector3.left)
            // Left Side Bowling  -> Swing Left to Right (Vector3.right)
            Vector3 direction = isBowlingRightSide ? Vector3.left : Vector3.right;

            rb.AddForce(direction * force, ForceMode.Force);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pitch") && !hasBounced)
        {
            HandleCricketBounce();
        }
    }

    // UPDATED BOWL FUNCTION WITH SIDE PARAMETER
    public void Bowl(Vector3 startPos, Vector3 targetPos, int type, float accuracy, bool isRightSide)
    {
        HardResetPhysics();
        
        transform.position = startPos;
        transform.rotation = Quaternion.identity;
        
        ballType = type;
        accuracyModifier = accuracy;
        inAir = true;
        hasBounced = false;
        isBowlingRightSide = isRightSide; // Store side
        rb.useGravity = true;

        if(trail) trail.Clear();

        //  Time Calculation
        Vector3 toTarget = targetPos - startPos;
        Vector3 horizontal = new Vector3(toTarget.x, 0, toTarget.z);
        float distance = horizontal.magnitude;
        float time = distance / deliverySpeed;

        //  SWING COMPENSATION LOGIC
        // We need to aim slightly opposite to the swing so it lands ON the marker.
        Vector3 finalTarget = targetPos;

        if (ballType == 1) 
        {
            float force = swingStrength * accuracyModifier;
            float mass = rb.mass;
            float acceleration = force / mass;
            float drift = 0.5f * acceleration * (time * time);

            // If Bowling Right (Swings Left) -> Aim Right (+ drift)
            // If Bowling Left (Swings Right) -> Aim Left (- drift)
            if (isBowlingRightSide)
                finalTarget.x += drift; 
            else
                finalTarget.x -= drift;
        }

        rb.linearVelocity = CalculateLaunchVelocity(startPos, finalTarget, deliverySpeed);
    }

    void HandleCricketBounce()
    {
        hasBounced = true;

        Vector3 incomingVelocity = lastFrameVelocity;
        Vector3 newVelocity = incomingVelocity;

        //  Bounce Up
        newVelocity.y = -newVelocity.y * bounciness;

        //  Apply Friction
        newVelocity.x *= (1f - gripLoss);
        newVelocity.z *= (1f - gripLoss);

        // TYPE SPECIFIC LOGIC
        if (ballType == 2) // SPIN
        {
            float angle = spinStrength * accuracyModifier; 
            // Invert spin direction based on bowling side
            if (!isBowlingRightSide) angle = -angle;
            
            Quaternion turn = Quaternion.Euler(0, angle, 0);
            newVelocity = turn * newVelocity;
        }
        else // SWING (Type 1)
        { 
            // By simply reflecting the vector, the ball naturally preserves 
            // its sideways momentum (Tangent) and continues drifting in that direction.
        }

        rb.linearVelocity = newVelocity;
    }

    Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 end, float speed)
    {
        Vector3 toTarget = end - start;
        Vector3 horizontal = new Vector3(toTarget.x, 0, toTarget.z);
        float distance = horizontal.magnitude;
        float time = distance / speed;

        float yStart = start.y;
        float gravity = Mathf.Abs(Physics.gravity.y);
        float vy = (0.5f * gravity * (time * time) - yStart) / time;

        Vector3 velocity = horizontal.normalized * speed;
        velocity.y = vy;
        return velocity;
    }
}