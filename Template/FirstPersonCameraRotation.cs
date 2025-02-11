using BepInEx;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngineInternal.Input;
using yandereMod;

/// <summary>
/// A simple FPP (First Person Perspective) camera rotation script.
/// Like those found in most FPS (First Person Shooter) games.
/// </summary>
public class FirstPersonCameraRotation : MonoBehaviour
{

    public float Sensitivity
    {
        get { return sensitivity; }
        set { sensitivity = value; }
    }
    [Range(0.1f, 9f)][SerializeField] float sensitivity = 2f;
    [Tooltip("Limits vertical camera rotation. Prevents the flipping that happens when rotation goes above 90.")]
    [Range(0f, 90f)][SerializeField] float yRotationLimit = 88f;

    Vector2 rotation = Vector2.zero;
    const string xAxis = "Mouse X"; //Strings in direct code generate garbage, storing and re-using them creates no garbage
    const string yAxis = "Mouse Y";
    PlayerActions playerActions;

    YandereEyeTarget yandere;
    Quaternion initRot;

    void Start()
    {
        playerActions = new PlayerActions();
        playerActions.Movement.Enable();
        yandere = FindFirstObjectByType<YandereEyeTarget>();
        transform.LookAt(yandere.eye.transform.position);
    }

    // Member variables (place these at the class level)
    private Vector2 offset;          // Accumulated rotation offset (in degrees)
    private Vector2 offsetVelocity;  // Current "velocity" of the offset

    // Tweak these values to adjust the spring behavior:
    public float sensitivity2 = 0.008f;
    public float springConstant = 10f;  // How strong is the pull back?
    public float damping = 5f;          // How much damping is applied (prevents oscillation)
    public float mass = 1f;             // Mass for the simulation (usually 1 works fine)

    public bool isInputDisabled = false;

    void Update()
    {
        // 1. Get player's mouse input and update the offset.
        //    (Assume offset.x is yaw and offset.y is pitch)
        Vector2 lookInput = playerActions.Movement.Look.ReadValue<Vector2>() *
                            sensitivity2 *
                            IngamePlayerSettings.Instance.settings.lookSensitivity;
        lookInput.y = -lookInput.y;

        if (isInputDisabled)
        {
            lookInput *= 0;
        }

        offset += lookInput;

        // Optionally clamp the offset so the player cannot look too far away.
        offset.x = Mathf.Clamp(offset.x, -40f, 40f);  // Yaw limits
        offset.y = Mathf.Clamp(offset.y, -20f, 20f);  // Pitch limits

        // 2. Compute the spring force.
        //    We want to pull the offset back toward zero (which corresponds to baseRotation).
        Vector2 springForce = -springConstant * offset;

        // 3. Compute damping force (which opposes the velocity).
        Vector2 dampingForce = -damping * offsetVelocity;

        // Total force and acceleration (F = m * a)
        Vector2 totalForce = springForce + dampingForce;
        Vector2 acceleration = totalForce / mass;

        // 4. Integrate the acceleration into the velocity and offset.
        offsetVelocity += acceleration * Time.deltaTime;
        offset += offsetVelocity * Time.deltaTime;

        // 5. Compute the camera's final rotation.
        //    The baseRotation could be, for example, a rotation that makes the camera look at a target.
        Quaternion baseRotation = Quaternion.LookRotation(yandere.eye.transform.position - transform.position);

        // Create a rotation from the offset (assuming offset.x is yaw and offset.y is pitch).
        // You can adjust the order or axis if your setup differs.
        Quaternion offsetRotation = Quaternion.Euler(offset.y, offset.x, 0f);

        // Combine baseRotation with the offset rotation.
        Quaternion finalRotation = baseRotation * offsetRotation;

        // Option 1: Directly set the rotation.
        transform.rotation = finalRotation;

        if (Mathf.Abs(offset.x) >= 35f || Mathf.Abs(offset.y) >= 15f)
        {
            playerActions.Movement.Disable();
            StartCoroutine(DisableInputTemporarily(0.5f));
        }

        //Quaternions seem to rotate more consistently than EulerAngles. Sensitivity seemed to change slightly at certain degrees using Euler. transform.localEulerAngles = new Vector3(-rotation.y, rotation.x, 0);
    }

    private System.Collections.IEnumerator DisableInputTemporarily(float duration)
    {
        isInputDisabled = true;
        playerActions.Movement.Disable();
        yield return new WaitForSeconds(duration);
        playerActions.Movement.Enable();
        isInputDisabled = false;
    }
}