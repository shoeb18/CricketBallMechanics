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
    private Vector3 lastFrameVelocity; 
    private TrailRenderer trail; // Auto-detects if you have a trail
    private bool isBowlingRightSide = true; // Store the side

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();
        
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        
        // Remove friction material to prevent conflicts
        if(GetComponent<Collider>())
            GetComponent<Collider>().material = null; 
    }

    // hard reset
    void HardResetPhysics()
    {
        // Toggling isKinematic ON and OFF forces Unity to forget 
        // all previous momentum, rotation, and collision cache.
        rb.isKinematic = true; 
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        lastFrameVelocity = Vector3.zero;
        rb.isKinematic = false; 
        
        rb.WakeUp(); // Force physics engine to pay attention to this object again
    }

    void FixedUpdate()
    {
        if (!inAir) return;

        // Track velocity for bounce calculation
        lastFrameVelocity = rb.linearVelocity;

        // === SWING LOGIC ===
        if (!hasBounced && ballType == 1) 
        {
            float force = swingStrength * accuracyModifier; 
            rb.AddForce(Vector3.right * force, ForceMode.Force);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only bounce on the Pitch
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
    
    // 1. Calculate how long the ball will be in the air
    Vector3 toTarget = targetPos - startPos;
    Vector3 horizontal = new Vector3(toTarget.x, 0, toTarget.z);
    float distance = horizontal.magnitude;
    float time = distance / deliverySpeed;

    // 2. SWING COMPENSATION LOGIC
    Vector3 finalTarget = targetPos;

    if (ballType == 1) // Swing
    {
        // F = ma  ->  a = F / m
        float force = swingStrength * accuracyModifier;
        
        float mass = rb.mass;
        float acceleration = force / mass;

        // Displacement = 0.5 * a * t^2
        float drift = 0.5f * acceleration * (time * time);

        finalTarget.x -= drift; 
    }

    rb.linearVelocity = CalculateLaunchVelocity(startPos, finalTarget, deliverySpeed);
}

    void HandleCricketBounce()
{
    hasBounced = true;

    Vector3 incomingVelocity = lastFrameVelocity;
    Vector3 newVelocity = incomingVelocity;

    // 1. Bounce Up
    newVelocity.y = -newVelocity.y * bounciness;

    // 2. Apply Friction
    newVelocity.x *= (1f - gripLoss);
    newVelocity.z *= (1f - gripLoss);

    // 3. TYPE SPECIFIC LOGIC
    if (ballType == 2) // SPIN
    {
        float angle = spinStrength * accuracyModifier; 
        
        // If we are on the Left side (Round the wicket for Right Hander), invert the spin
        if (!isBowlingRightSide) 
        {
            angle = -angle;
        }

        Quaternion turn = Quaternion.Euler(0, angle, 0);
        newVelocity = turn * newVelocity;
    }
    else // SWING
    {
        // STRICT REQUIREMENT: Go straight forward after bounce.
        newVelocity.x = 0f;
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