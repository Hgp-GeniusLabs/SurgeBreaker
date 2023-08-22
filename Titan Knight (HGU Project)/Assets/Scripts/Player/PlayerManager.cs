/* Mark Gaskins
 * THIS SHOULD BE ATTATCHED TO THE "PLAYER CONTROLLER" OBJECT AT ALL TIMES
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(CharacterController))]
public class PlayerManager : MonoBehaviour
{
    [Space(20)]
    [Header("Player Preferences")]

    [Tooltip("The amount of currency that the player starts with.")] [Range(150, 3999)] public int startCurrency = 275; // Do not use this to get a reference to her current amount of currency.

    [Tooltip("The walkspeed of the player")] [Range(4.0f, 12.0f)] public float speed = 6.0f;
    [Tooltip("" +
        "The side walkspeed of the player. The higher this value, the slower the side speed.\n" +
        "(0 > Disabled, 5 > Default Speed, 9.5 > Fastest Recommended Speed) ")]
    [Range(2.0f, 9.5f)] public float sideSpeedMultiplier = 5;
    [Tooltip("The walkspeed multiplayer when the player's running")] [Range(0.1f, 4.5f)] public float runSpeedMultiplier = 1.5f;
    [Tooltip("The jump force")] [Range(1.0f, 12)] public float jumpPower = 8.0f;
    [Tooltip("Gravity's influence on the player")] [Range(10.0f, 50.0f)] public float gravity = 20.0f;
    [Tooltip("The speed in which the player rotates when input is recieved")] [SerializeField] [Range(300, 1000)] float pModelRotSpeed = 800;
    [Space(5)]
    //[Tooltip("The amount of health that the player starts with. (default: 100)")] [SerializeField] private int startPlayerHealth = 100; // Do not use this to reference current player health.
    //[Tooltip("A non-static reference for the current Player Health. (read values only!)")] public int currentPlayerHealth;


    [Space(10)]
    [Header("Camera Preferences")]
    [Tooltip("Should the Camera FOV change when the player sprints?")] [SerializeField] public bool doSprintingFOV = true;
    [Tooltip("The sprinting FOV of the Camera. (FOV changes to this, when player sprints.)")] [Range(60, 100)] public float sprintingFOV = 90;
    [Tooltip("The standard FOV of the Camera. (FOV reverts to this, when not sprinting.)")] [Range(60, 100)] public float normalFOV = 65;
    [Tooltip("The speed in which FOV changes when the player sprints, or stops sprinting.")] [SerializeField] [Range(0, 20)] int smoothFOVSpeed = 10;

    [Space(26)]
    [Header("Player References")]
    [Tooltip("The parent object of the player model, which rotates the player model in the direction that the player is facing.")] [SerializeField] Transform pModelRotation;
    [Tooltip(("Reference to player's Animator component "))] public Animator pAnimator;

    [Space(20)]
    [Header("Camera References")]
    [Tooltip("This is the player camera. It has a Fixed Position!")] [SerializeField] GameObject pCamera; // The pos/rot of this will update with the pCamera_Position.
    [Tooltip("This is the game object that updates the position and rotation of the pCamera.")] [SerializeField] GameObject pCamera_Position;

    [Space(20)]
    [Header("Cursor References")] // Handle the crosshair switching in a canvas to support animation of the crosshair.
    [Tooltip("Should the game use the dynamic cursors?\n This hides the default cursor and replaces it with one determined by the current mode.")] [SerializeField] public bool doDynamicCursors;
    [Space(8)]
    [Tooltip("Parent of the dynamic cursors.")] [SerializeField] public RectTransform CursorsParent;
    [Tooltip("The default cursor. Appears in idle mode.")] [SerializeField] GameObject DefaultCursor; // (Default Cursor)
    [Tooltip("The build cursor. Appears in build mode.")] [SerializeField] GameObject BuildCursor; // (Build mode)
    [Tooltip("The crosshair cursor. Appears in combat mode.")] [SerializeField] GameObject CrosshairCursor; // (Combat mode)

    [Space(20)]
    [Header("Misc. References")]
    [Tooltip("This is currently a Screen Space canvas, but later it should be modified to be a World Space canvas near the player.")] [SerializeField] Slider healthDisplay;
    [Tooltip("Later, this should be a sort of armor indicator. Currently, it is a second health display.")] [SerializeField] GameObject[] ArmorIcons;
    [SerializeField] TextMeshProUGUI healthText, currencyText;




    [Space(5)]

    private CharacterController controller;

    private Health playerHealth; // This manages all player health.

    private bool isRunning;
    float verticalSpeed;

    private GameManager gm;

    public static bool isDead, generatorsDestroyed;
    public static int currentCurrency; // This constantly updates, and should be used to get the current amount of money that she has.

    [HideInInspector] public Vector3 movementDirection;

    private void Start()
    {
        isDead = false;
        generatorsDestroyed = false;
        AudioListener.pause = false;
    }

    private void Awake() // Assign defaults
    {
        controller = GetComponent<CharacterController>();
        playerHealth = GetComponent<Health>();
        //pAnimator = GetComponent<Animator>();
        gm = FindObjectOfType<GameManager>();

        Camera.main.fieldOfView = normalFOV;

        currentCurrency = startCurrency;
    }

    public void Die() // Automatically called when the player dies. This is just for the proto-stage of Titan Knights.
    {
        Debug.Log($"<color=\"red\"><b>Player has died!</b></color>");

        isDead = true;
    }


    private void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Pressing R will reload the scene. Might need to be discontinued soon.

        float zInput = Input.GetAxis("Vertical");    // Forward/Backward Movement
        float xInput = Input.GetAxis("Horizontal");  // Left/Right Movement

        HandleMovement(xInput, zInput);
        HandleSprinting();
        HandleAnimations(xInput, zInput);
        HandleDynamicCursor();
        HandleLosing();
        HandleHealthUI();

        if (FindObjectOfType<GameManager>().doInfiniteMoney) { currencyText.text = "inf."; currentCurrency = 99999999; }
        else currencyText.text = $"${currentCurrency}";

    }


    private void HandleHealthUI()
    {
        // Update temporary health display (shown for debug reasons)
        healthDisplay.value = playerHealth.currentHealth;
        healthDisplay.maxValue = playerHealth.startHealth;
        healthText.text = $"{playerHealth.currentHealth}%";

        if (playerHealth.currentHealth >= 66) ArmorIcons[0].SetActive(true); else ArmorIcons[0].SetActive(false);
        if (playerHealth.currentHealth >= 33) ArmorIcons[1].SetActive(true); else ArmorIcons[1].SetActive(false);
        if (playerHealth.currentHealth >= 10) ArmorIcons[2].SetActive(true); else ArmorIcons[2].SetActive(false);
    }
    private void HandleLosing()
    {
        if (generatorsDestroyed)
        {
            controller.enabled = false;
            this.enabled = false;

            FindObjectOfType<GameManager>().GameOver();
        }

        if (isDead)
        {
            //pModelRotation.gameObject.SetActive(false);

            controller.enabled = false;
            this.enabled = false;

            FindObjectOfType<GameManager>().GameOver();
        }
    }
    private void HandleDynamicCursor() // Handle the crosshair switching in a canvas to support animation of the crosshair.
    {
        if (doDynamicCursors)
        {
            Cursor.visible = false;

            CursorsParent.position = Input.mousePosition;

            if (gm.currentMode == GameMode.Build) // Show the build crosshair if the current mode is build mode
            {
                BuildCursor.SetActive(true);

                DefaultCursor.SetActive(false);
                CrosshairCursor.SetActive(false);
            }
            else if (gm.currentMode == GameMode.Idle)  // Show the default (idle) crosshair if the current mode is idle mode
            {
                DefaultCursor.SetActive(true);

                BuildCursor.SetActive(false);
                CrosshairCursor.SetActive(false);
            }
            else if (gm.currentMode == GameMode.Combat)  // Show the combat crosshair if the current mode is combat mode
            {
                CrosshairCursor.SetActive(true);

                DefaultCursor.SetActive(false);
                BuildCursor.SetActive(false);
            }
        }
        else // Hide the dynamic cursor if doDynamicCursors is false.
        {
            BuildCursor.SetActive(false);
            DefaultCursor.SetActive(false);
            CrosshairCursor.SetActive(false);

            Cursor.visible = true;
        }
    }

    private void HandleSprinting()
    {
        // Handle sprinting 
        if (!isRunning) // Not sprinting
        {
            if (doSprintingFOV && Camera.main.fieldOfView > normalFOV) // Smoothly transition back to normal FOV, at smoothFOVSpeed speed
                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, normalFOV, smoothFOVSpeed * Time.deltaTime);

            controller.Move(movementVelocity * Time.deltaTime);

        }
        else // Sprinting 
        {
            if (doSprintingFOV && Camera.main.fieldOfView < sprintingFOV) // Smoothly transition to sprinting FOV, at smoothFOVSpeed speed
                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, sprintingFOV, smoothFOVSpeed * Time.deltaTime);


            controller.Move(new Vector3(
            movementDirection.x * (speed * runSpeedMultiplier),
            movementDirection.y * speed, /* Prevent the jump power from multiplying! */
            movementDirection.z * (speed * runSpeedMultiplier)) * Time.deltaTime);
            //pAnimator.SetFloat("forwardMovement", zInput);
        }
    }


    private Vector3 movementVelocity;
    private void HandleMovement(float xInput, float zInput) // The movement will be completely rescripted in order to rotate movement grid by 45°
    {
        if (GameManager.hasWon) return; // Player will not be allowed to be controlled if they have already won.

        if (Input.GetKey(KeyCode.LeftShift)) isRunning = true; else isRunning = false; // Handle sprinting with left shift


        // Make the player face the quadrant of the screen that the mouse is in
        Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayLength;

        if (groundPlane.Raycast(cameraRay, out rayLength))
        {
            Vector3 pointToLook = cameraRay.GetPoint(rayLength);
            Debug.DrawLine(cameraRay.origin, pointToLook, Color.cyan);

            pModelRotation.transform.LookAt(new Vector3(pointToLook.x, transform.position.y, pointToLook.z));
        }

        // Make the player move in the direction of the pModelRotation.
        movementDirection = pModelRotation.transform.forward * zInput + pModelRotation.transform.right * xInput - (pModelRotation.transform.right * (xInput / sideSpeedMultiplier));

        movementVelocity = movementDirection * speed;

    }

    private void HandleAnimations(float xInput, float zInput)
    {
        /// [1.9a NOTICE] Horizontal animations for walking are required, as this code is now deprecated as of version 1.9a
        if (isDead) pAnimator.SetTrigger("Die");

        pAnimator.SetFloat("forwardMovement", zInput);
        pAnimator.SetFloat("strafingMovement", xInput);
        pAnimator.SetBool("isShooting", Input.GetMouseButton(0));
    }
}
