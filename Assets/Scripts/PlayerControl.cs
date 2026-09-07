using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent, RequireComponent(typeof(CharacterController))] //[RequireComponent(typeof(Rigidbody))]
public class PlayerControl : MonoBehaviour
{
	[Header("Camera")]
	[SerializeField] private Camera playerCamera;
	[HideInInspector] public Camera PlayerCamera { get => playerCamera; }
	[SerializeField, Min(0.0f)] private float camRotSensitivity = 0.1f;
	[SerializeField, Range(0.0f, 1.0f)] private float camRotSmooth = 0.001f;
	[SerializeField] private bool disableCameraTiltOnRot = true;
	private Vector3 targetCamRot = new();

	[Header("Moving")]
	[SerializeField, Min(0.0f)] private float moveSpeed = 4.0f;
	[SerializeField, Range(0.0f, 1.0f)] private float moveAccelSmooth = 0.001f;
	[SerializeField] private float YposMoveTowards = 0.1f;
	private CharacterController cc;
	//private Rigidbody rb;
	private Vector3 targetMoveVel = new();
	private Vector3 currMoveVel;
	private Vector3 playerStartPosWorld;
	private Quaternion playerStartRotWorld;
	private Vector3 camStartPosWorld;
	private Quaternion camStartRotWorld;

	[Header("Input")]
	[SerializeField] private InputActionReference playerMoveAct;
	[SerializeField] private InputActionReference playerLookAct;
	[SerializeField] private InputActionReference playerInteractAct;
	[SerializeField, Min(0.0f)] private float maxInteractDist = 3.0f;
	[SerializeField] private LayerMask playerInteractLayers;
	[SerializeField] private InputsHUD _inputHUD;

	[SerializeField] private GameObject borderWarningUI;

	[SerializeField] private GameObject worldBorder;
	[Tooltip("The minimum distance (from the world border's pivot) to start showing the border warning.")]
	[SerializeField] private float borderWarnDist;

	[Header("Exposed values, not settings")]
	[SerializeField] private bool holdingInteractButton;
	private IPlayerInteractable currLookingAtInteractable;
	private IPlayerInteractable currInteractingWith;
	public string CurrentControlScheme;

	#region Singleton
	//private static InputSystem_Actions actionMap;
	//[HideInInspector] public static InputSystem_Actions ActionMap { get { return actionMap; } }

	private static PlayerControl instance;
	[HideInInspector]
	public static PlayerControl Instance
	{
		get { return instance; }
		private set
		{
			// if the singleton doesn't already have an instance (or we can replace it)
			if(instance == null)
			{
				instance = value;
				instance.InitSingleton();
				return;
			}
			// if we are the instance
			if(instance == value)
			{
				return;
			}
			Debug.Log($"Note: \"{nameof(PlayerControl)}\" instance exists, destroying new one!");
			// destroy the new component instance's GameObject
			//   (because the whole prefab is one instance).
			Destroy(value.gameObject);
		}
	}
	/// <summary>
	/// To be used instead of checking Instance == null,
	/// as this can be hijacked for testing purposes.
	/// </summary>
	public static bool InstanceExists() => (instance != null);
	#endregion

	#region Singleton Initialisation
	// Awake() is called the moment the object is active in the scene
	protected virtual void Awake()
	{
		//base.Start();
		// set the instance to this, which will call InitSingleton()
		Instance = this;
	}
	/// <summary>
	/// Acts as a stand-in for Start()/Awake(), should only be called by the Singleton.
	/// </summary>
	private void InitSingleton()
	{
		//actionMap = new InputSystem_Actions();

		// Safe because required component
		cc = GetComponent<CharacterController>();
		//rb = GetComponent<Rigidbody>();
		playerStartPosWorld = transform.position;
		playerStartRotWorld = transform.rotation;
		camStartPosWorld = playerCamera.transform.position;
		camStartRotWorld = playerCamera.transform.rotation;
	}
	#endregion

	private void OnEnable()
	{
		playerMoveAct.action.Enable();
		playerLookAct.action.Enable();
		playerInteractAct.action.Enable();
		playerInteractAct.action.started += (ctx) => PlayerInteract();
		playerInteractAct.action.canceled += (ctx) => PlayerStopInteract();

		playerMoveAct.action.performed += UpdateControlScheme;
		playerLookAct.action.performed += UpdateControlScheme;
		playerInteractAct.action.performed += UpdateControlScheme;
	}
	private void OnDisable()
	{
		playerMoveAct.action.Disable();
		playerLookAct.action.Disable();
		playerInteractAct.action.Disable();
		playerInteractAct.action.started -= (ctx) => PlayerInteract();
		playerInteractAct.action.canceled -= (ctx) => PlayerStopInteract();

		playerMoveAct.action.performed -= UpdateControlScheme;
		playerLookAct.action.performed -= UpdateControlScheme;
		playerInteractAct.action.performed -= UpdateControlScheme;
	}

	// Update is called once per frame
	private void Update()
	{
		RotateCamera();
		MovePlayer();
		TryFindInteractable();
		CheckIfShowBorderWarning();
	}

	private void RotateCamera()
	{
		// Get current mouse input delta
		Vector2 mouseAxis = playerLookAct.action.ReadValue<Vector2>();

		// change the desired camera rotation
		targetCamRot.x -= mouseAxis.y * camRotSensitivity;
		targetCamRot.y += mouseAxis.x * camRotSensitivity;
		targetCamRot.z = 0.0f; // the desired cam rot has no tilt

		// clamp to between straight up and straight down
		targetCamRot.x = Mathf.Clamp(targetCamRot.x, -90.0f, 90.0f);

		// wrap horizontal angle
		if(targetCamRot.y < 0.0f) { targetCamRot.y += 360.0f; }
		if(targetCamRot.y > 360.0f) { targetCamRot.y -= 360.0f; }

		// unshackle the slerp smoothing from the framerate
		float t = 1.0f - Mathf.Pow(camRotSmooth, Time.deltaTime);

		// Spherically lerp the camera's rotation towards the desired rotation. At smoothing = 0, no
		//   smoothing is applied. At smoothing = 1, so much smoothing is applied that nothing happens.
		playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, Quaternion.Euler(targetCamRot), t);

		if(disableCameraTiltOnRot)
		{
			// remove the camera roll caused by quaternion rotation
			Vector3 tmp = playerCamera.transform.eulerAngles;
			tmp.z = 0.0f;
			playerCamera.transform.eulerAngles = tmp;
		}

		//Vector3 currRot = Vector3.Slerp(playerCamera.transform.eulerAngles, targetCamRot, t / camRotSmooth);
		//transform.eulerAngles = new Vector3(0.0f, currRot.y, 0.0f);
		//currRot.y = 0.0f;
		//if(disableCameraTiltOnRot) { currRot.z = 0.0f; }
		//playerCamera.transform.localEulerAngles = currRot;
	}

	private void MovePlayer()
	{
		// get input
		targetMoveVel = playerMoveAct.action.ReadValue<Vector2>();
		// swap the y and z components, to move laterally
		targetMoveVel.z = targetMoveVel.y;
		// Set the vertical velocity such that the player moves towards the set Y position.
		// If the distance to move is less than the character controller's minMoveDistance, nothing will happen.
		targetMoveVel.y = YposMoveTowards - transform.position.y;

		// rotate the move vector based on the yaw of the camera
		targetMoveVel = Quaternion.AngleAxis(playerCamera.transform.eulerAngles.y, Vector3.up) * targetMoveVel;

		// unshackle the lerp smoothing from the framerate
		float t = 1.0f - Mathf.Pow(moveAccelSmooth, Time.deltaTime);

		// Lerp the current velocity towards the desired velocity. At smoothing = 0, no smoothing
		//   is applied. At smoothing = 1, so much smoothing is applied that nothing happens.
		currMoveVel = Vector3.Lerp(cc.velocity, targetMoveVel * moveSpeed, t);

		//Debug.Log($"cc.velocity {cc.velocity}    targetMoveVel {targetMoveVel * moveSpeed}     currMoveVel {currMoveVel}");

		// set the player's velocity
		cc.Move(currMoveVel * Time.deltaTime);
	}

	public void ResetPosRot()
	{
		transform.SetPositionAndRotation(playerStartPosWorld, playerStartRotWorld);
		targetMoveVel = playerStartPosWorld;
		playerCamera.transform.SetPositionAndRotation(camStartPosWorld, camStartRotWorld);
		targetCamRot = camStartRotWorld.eulerAngles;
		PlayerStopInteract();
	}

	private Transform interRef;
	private bool success;
	private void TryFindInteractable()
	{
		// Don't try to find a new thing to interact with if we're
		//   currently already busy interacting with something
		if(holdingInteractButton) { return; }

		// Clear reference to previous interactable
		currLookingAtInteractable = null;
		// try to find an interactable object
		success = Physics.Raycast(
			origin: playerCamera.transform.position,
			direction: playerCamera.transform.forward,
			out RaycastHit hitInfo, // store the results in this newly-declared variable
			maxDistance: maxInteractDist,
			layerMask: playerInteractLayers
		);

		// If we hit something, recursively try to find an interactible script, going up the hit object's heirachy tree.
		if(success)
		{
			interRef = hitInfo.transform;
			do // a do-while loop is just a while loop but checks the condition AFTER the body is run.
			{
				// Try to get the interactable script
				if(interRef.TryGetComponent<IPlayerInteractable>(out IPlayerInteractable inter))
				{
					// Save a reference to the interactable object
					currLookingAtInteractable = inter;

					//Debug.Log("CAN INTERACT WITH: " + currLookingAtInteractable);
					break; // break out of while loop
				}

				// Go to the parent and try again. If no parent (is null), then the
				//   while condition will fail and will continue on with the function.
				interRef = interRef.parent;
			}
			while(interRef != null);
		}

		// After this point, we know for sure if we are looking at an interactable or not.

		// If we're looking at an interactable, show the prompt, otherwise don't.
		_inputHUD.SetInteractPrompt(currLookingAtInteractable != null);
	}

	private void PlayerInteract()
	{
		// If there's nothing to interact with, stop here
		if(currLookingAtInteractable == null) { return; }

		// If we are somehow interacting with something already, quickly stop interacting with it
		if(currInteractingWith != null)
		{
			Debug.LogWarning($"Warning: Somehow interacting with two! Old: {currInteractingWith}, New: {currLookingAtInteractable}");
			PlayerStopInteract();
			// currInteractingWith should now be null
		}

		// Remember that we are interacting with it
		currInteractingWith = currLookingAtInteractable;

		currInteractingWith.Interact(); // Interact with it
		holdingInteractButton = true;

		Debug.Log("Interacted with " + currInteractingWith);
	}

	private void PlayerStopInteract()
	{
		holdingInteractButton = false;

		// If we weren't interacting with anything in the first place, leave
		if(currInteractingWith == null) { return; }

		// Inform the interactable that we have stopped interacting with it
		currInteractingWith.StopInteract();
		Debug.Log("Stopped interacting with " + currInteractingWith);

		// Finished doing stuff with this, forget it
		currInteractingWith = null;
		currLookingAtInteractable = null;
	}


	//after an action is performed it detects which control scheme is the current and sends a command to show the corresponding HUD
	private void UpdateControlScheme(InputAction.CallbackContext ctx)
	{
		InputDevice device = ctx.control.device;

		string newScheme = CurrentControlScheme;

		if(device is Gamepad)
		{
			newScheme = "Gamepad";
		}
		else if(device is Keyboard || device is Mouse)
		{
			newScheme = "Keyboard&Mouse";
		}
		else
		{
			//(missing yet to be implemented)
			_inputHUD.TogglePanel("Missing");
		}

		if(newScheme == CurrentControlScheme)
		{
			return;
		}

		CurrentControlScheme = newScheme;
		_inputHUD.TogglePanel(newScheme);
	}

	private Vector3 toCenterVec;
	private Vector3 playerFwdVec;
	private float angleDot;
	private void CheckIfShowBorderWarning()
	{
		// Get the vector from the player's position to the center.
		toCenterVec = (worldBorder.transform.position - transform.position);
		toCenterVec.y = 0.0f; // Vertically-flatten vector

		// If we are still considered within range, don't show the border warning.
		if(toCenterVec.sqrMagnitude < (borderWarnDist * borderWarnDist))
		{
			borderWarningUI.SetActive(false);	
			return;
		}

		// Get the facing direction of the player.
		playerFwdVec = playerCamera.transform.forward;
		playerFwdVec.y = 0.0f; // Vertically-flatten vector

		// Calculate the dot product of the two vectors.
		// NOTE that they are not normalised, and so the only accurate part of the
		//   variable is whether it is less than, greater than, or equal to zero.
		// In this case, that's all we care about, so there's no point normalising.
		angleDot = Vector3.Dot(playerFwdVec, toCenterVec);

		// If the dot product is less than zero, we must be
		//   in the semicircle facing AWAY from the center.
		borderWarningUI.SetActive(angleDot <= 0.0f);
	}

	private const float DONT_UPDATE_VAL = float.NegativeInfinity;
	public void UpdateSpeedValues(float moveSpd = DONT_UPDATE_VAL, float moveAccel = DONT_UPDATE_VAL,
		float camRotSens = DONT_UPDATE_VAL, float camRotSmth = DONT_UPDATE_VAL)
	{
		if(moveSpd != DONT_UPDATE_VAL)       {  moveSpeed = moveSpd;              }
		if(moveAccel != DONT_UPDATE_VAL) {  moveAccelSmooth = moveAccel;  }
		if(camRotSens != DONT_UPDATE_VAL)    {  camRotSensitivity = camRotSens;   }
		if(camRotSmth != DONT_UPDATE_VAL)    {  camRotSmooth = camRotSmth;        }
	}


#if UNITY_EDITOR // Gizmos don't exist outside of the editor, so just remove from binaries entirely
	private void OnDrawGizmos()
	{
		if(worldBorder == null) { return; } // Don't run if missing the reference.

		// Draw the bounds of the border warning. If the player
		//   is outside this sphere, the border warning will show.
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(worldBorder.transform.position, borderWarnDist);
	}
#endif
}