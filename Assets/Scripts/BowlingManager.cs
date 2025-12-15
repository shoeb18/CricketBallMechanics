using UnityEngine;
using UnityEngine.UI; 

public class BowlingManager : MonoBehaviour
{
    [Header("Scene References")]
    public CricketBallController ballScript;
    public Transform markerTransform; 
    public Transform bowlerHand; 

    [Header("UI References")]
    public Slider powerSlider;
    public Text ballInfoText; 

    [Header("Settings")]
    public float markerSpeed = 5f;
    public float sliderSpeed = 2.5f; 
    public float wicketOffset = 0.6f; 

    // State Variables
    private int currentType = 1;         // 1=Swing, 2=Spin
    private bool isRightSide = true;     
    private bool isBowling = false;      
    private float sliderTimer = 0f;

    void Start()
    {
        UpdateUIText();
        ResetDelivery();
    }

    void Update()
    {
        // If ball is moving, disable controls
        if (isBowling) return;

        // Inputs
        HandleMarkerMovement();
        HandleSideSelection();
        HandleTypeSelection();

        // Slider
        sliderTimer += Time.deltaTime * sliderSpeed;
        powerSlider.value = Mathf.PingPong(sliderTimer, 1.0f);

        // on Space pressed Lock & Bowl
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isBowling = true;
            PerformBowl(); 
        }
    }

    void PerformBowl()
    {
        // Calculate Accuracy: Center (0.5) is perfect (1.0)
        float val = powerSlider.value;
        float accuracy = 1.0f - Mathf.Abs(val - 0.5f); 

        // Fire the Ball
        ballScript.Bowl(bowlerHand.position, markerTransform.position, currentType, accuracy, isRightSide);

        // Reset system after 4 seconds for next ball
        Invoke("ResetDelivery", 4f);
    }

    void ResetDelivery()
    {
        isBowling = false;
        sliderTimer = 0f;
    }

    
    void HandleMarkerMovement()
    {
        float x = Input.GetAxis("Horizontal"); // A / D
        float z = Input.GetAxis("Vertical");   // W / S
        
        Vector3 move = new Vector3(x, 0, z) * markerSpeed * Time.deltaTime;
        Vector3 newPos = markerTransform.position + move;
        
        // Keep Marker on the Pitch
        newPos.x = Mathf.Clamp(newPos.x, -1.5f, 1.5f);
        newPos.z = Mathf.Clamp(newPos.z, 2.0f, 18.0f);
        
        markerTransform.position = newPos;
    }

    void HandleSideSelection()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) { isRightSide = false; UpdateBowlerPos(); }
        if (Input.GetKeyDown(KeyCode.RightArrow)) { isRightSide = true; UpdateBowlerPos(); }
    }

    void HandleTypeSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { currentType = 1; UpdateUIText(); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { currentType = 2; UpdateUIText(); }
    }

    void UpdateBowlerPos()
    {
        Vector3 p = bowlerHand.position;
        p.x = isRightSide ? wicketOffset : -wicketOffset;
        bowlerHand.position = p;
        UpdateUIText();
    }

    void UpdateUIText()
    {
        if(!ballInfoText) return;
        string side = isRightSide ? "Over Wicket" : "Round Wicket";
        string type = (currentType == 1) ? "SWING" : "SPIN";
        ballInfoText.text = $"{side} | {type}\n[WASD] Aim   [Space] Bowl";
    }
}