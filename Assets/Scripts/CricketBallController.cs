using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CricketBallController : MonoBehaviour
{
    [Header("Delivery Settings")]
    public float deliverySpeed = 25f; // Forward speed (m/s)
    
    [Header("Physics Tuning")]
    [Tooltip("Curve strength. Default: 2.0. Can handle 50+ now.")]
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
    private bool isBowlingRightSide = true;

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
            
            // Swing Direction depends on bowling side
            // Right Side -> Swing Left (-X)
            // Left Side  -> Swing Right (+X)
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

    public void Bowl(Vector3 startPos, Vector3 targetPos, int type, float accuracy, bool isRightSide)
    {
        HardResetPhysics();
        
        transform.position = startPos;
        transform.rotation = Quaternion.identity;
        
        ballType = type;
        accuracyModifier = accuracy;
        inAir = true;
        hasBounced = false;
        isBowlingRightSide = isRightSide;
        rb.useGravity = true;

        if(trail) trail.Clear();

        // trajectory calculation

        // Calculate Time based ONLY on Forward Speed (Z-Axis)
        // This prevents the "Fly Away" bug. Time is now constant regardless of curve.
        float zDist = targetPos.z - startPos.z;
        float time = zDist / deliverySpeed; 

        // Calculate Vertical Velocity (Vy) needed to counteract Gravity
        // Formula: y = y0 + vy*t - 0.5*g*t^2
        float gravity = Mathf.Abs(Physics.gravity.y);
        float yDist = targetPos.y - startPos.y;
        float vy = (yDist + 0.5f * gravity * time * time) / time;

        // Calculate Horizontal Velocity (Vx) needed to counteract Swing
        // Formula: x = x0 + vx*t + 0.5*a*t^2
        // We rearrange to find vx:  vx = (xTarget - xStart - 0.5*a*t^2) / t
        float vx = 0f;

        if (ballType == 1) // Swing Compensation
        {
            float force = swingStrength * accuracyModifier;
            // Determine acceleration direction (+ or -)
            // Right Side bowls Left (-1), Left Side bowls Right (+1)
            float direction = isBowlingRightSide ? -1f : 1f; 
            float acceleration = (force / rb.mass) * direction;

            float xDist = targetPos.x - startPos.x;
            float drift = 0.5f * acceleration * time * time;

            vx = (xDist - drift) / time;
        }
        else // Straight / Spin (No air curve)
        {
            float xDist = targetPos.x - startPos.x;
            vx = xDist / time;
        }

        // Apply Final Velocity Vector
        // We construct the vector manually instead of using "transform.forward"
        Vector3 finalVelocity = new Vector3(vx, vy, deliverySpeed);
        rb.linearVelocity = finalVelocity;
    }

    void HandleCricketBounce()
    {
        hasBounced = true;

        Vector3 incomingVelocity = lastFrameVelocity;
        Vector3 newVelocity = incomingVelocity;

        // Bounce Up
        newVelocity.y = -newVelocity.y * bounciness;

        // Apply Friction
        newVelocity.x *= (1f - gripLoss);
        newVelocity.z *= (1f - gripLoss);

        // TYPE SPECIFIC LOGIC
        if (ballType == 2) // SPIN
        {
            float angle = spinStrength * accuracyModifier; 
            if (!isBowlingRightSide) angle = -angle;
            
            Quaternion turn = Quaternion.Euler(0, angle, 0);
            newVelocity = turn * newVelocity;
        }

        // for swing
        // Tangential path is preserved automatically because 
        // we are NOT setting X to 0. The ball keeps its sideways momentum.

        rb.linearVelocity = newVelocity;
    }
}