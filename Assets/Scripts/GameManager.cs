using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
	public enum GameState
	{
		Active,
		SecurityMenu,
		AreYouThere,
		LoadingTransition,
		RebindingMenu,
	};

	//https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html
	//[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	//private static void BeforeSceneLoad() {}

	[Header("Transition Settings")]
	[SerializeField] private Canvas hud;
	[SerializeField] private Graphic transitionBlackout;
	[SerializeField] private Color sceneTransitionBackgroundColour;
	private Color noColour = Color.clear;
	[SerializeField, Min(0f)] private float fadeInSeconds;
	[HideInInspector] public float FadeInSeconds { get { return fadeInSeconds; } }
	[SerializeField, Min(0f)] private float fadeOutSeconds;
	[HideInInspector] public float FadeOutSeconds { get { return fadeOutSeconds; } }
	
	[Header("Timeout Settings")]
	[Tooltip("If no user input occurs for this many seconds in a row, activate the timeout.")]
	[SerializeField] private float timeoutWaitSecs = 5.0f * 60.0f; // default 5 minutes
	[Tooltip("The number of seconds between re-checking the number of visible plaques.")]
	[SerializeField, Min(0.0f)] private float secsBetweenCountVisiblePlaques = 1.0f;
	[SerializeField] private GameObject areYouTherePromptScreen;
	[Tooltip("Only the input actions listed here can reset the timeout timer.")]
	[SerializeField] private InputActionReference[] inputsThatResetTimeoutTimer = new InputActionReference[0];

	[Header("Exposed values, not settings")]
	[SerializeField] private GameState state = GameState.Active;
	[HideInInspector] public GameState CurrentGameState { get => state; }
	//private GameObject currMenuRoomLoaded;
	[SerializeField] private SecurityMenu currOpenSecurityMenu;
	[Space(5)]
	[SerializeField] private bool timeoutDisabled = false;
	[SerializeField] private float timeoutTimer;
	[SerializeField] private Plaque[] plaquesFoundInScene = new Plaque[0];
	[SerializeField] private float currSecsBetweenCountVisiblePlaques = 0.0f;
	[SerializeField] private int numPlaquesVisible = 0;

#region Singleton
	private static GameManager instance;
	[HideInInspector] public static GameManager Instance
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
			Debug.Log($"Note: \"{nameof(GameManager)}\" instance exists, destroying new one!");
			// destroy the new component instance's GameObject
			//   (because the whole prefab is one instance).
			Destroy(value.gameObject);
		}
	}

	/// <summary>
	/// To be used instead of checking Instance == null,
	/// as this can be hijacked for testing purposes.
	/// </summary>
	public static bool InstanceExists() { return (instance != null); }

#endregion
	
#region Singleton Initialisation
	// Awake() is called the moment the object is active in the scene
	private void Awake()
	{
		// set the instance to this, which will call InitSingleton()
		Instance = this;
	}
	/// <summary>
	/// Acts as a stand-in for Start()/Awake(), should only be called by the Singleton.
	/// </summary>
	private void InitSingleton()
	{
		if(transitionBlackout == null)
		{  Debug.LogError($"Error: {this} has no {nameof(transitionBlackout)} assigned!");  }
		transitionBlackout.gameObject.SetActive(false);
		if(!transitionBlackout.material.shader.isSupported)
		{
			Debug.LogError($"Error: {nameof(Shader)} \"{transitionBlackout.material.shader.name}\" is not supported on the end-user's graphics card!");
		}

		if(areYouTherePromptScreen == null)
		{  Debug.LogError($"Error: {this} has no {nameof(areYouTherePromptScreen)} assigned!");  }
	}
#endregion

	// Start() is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
	{
		// Timeout starts enabled, will disable if anything goes wrong, and won't re-enable.
		timeoutDisabled = false;
		// Set the listeners for the timeout timer
		SetListenersForInput();

		// Find and listen to the opening and closing of security menus
		ListenForSecurityMenus();

		ListenForRebindingMenu();

		// Make everything black
		SceneTransitionCut(sceneTransitionBackgroundColour, true);

		// Load the world
		StartCoroutine(ReloadWorld());
	}

	// Update is called once per frame
	private void Update()
	{
		// Run timeout functionality
		CheckTimeout();
	}

#region World Loading

	public void ResetEverything()
	{
		if(state != GameState.Active)
		{
			// Can't reset during security menus or transitions (which includes being in the process of resetting)
			return;
		}

		StartCoroutine(ReloadWorld());
	}

	private IEnumerator ReloadWorld()
	{
		Debug.Log("Reloading World...");

		UpdateState(GameState.LoadingTransition);

		// fade to black, don't continue until finished
		yield return FadeToBlack(fadeOutSeconds);

		// place level, don't continue until finished
		yield return StartCoroutine(PiTManager.Instance.PlaceAllPiTs(gameObject.scene));

		// Reset the position and rotation of the player
		if(PlayerControl.InstanceExists())
		{
			PlayerControl.Instance.ResetPosRot();
		}

		// Re-scan for plaques for timeout input detection
		ScanForPlaques();

		// Fade from black, don't continue until finished
		yield return FadeFromBlack(fadeInSeconds);
		
		UpdateState(GameState.Active);
	}

#endregion

#region Game State

	private void UpdateState(GameState newState)
	{
		state = newState;
		
		Time.timeScale = (state == GameState.Active || state == GameState.LoadingTransition) ? 1.0f : 0.0f;

		hud.gameObject.SetActive(state == GameState.Active);
		areYouTherePromptScreen.SetActive(state == GameState.AreYouThere);

		Cursor.visible = (state == GameState.SecurityMenu || state == GameState.AreYouThere || state == GameState.RebindingMenu);
		//if(state == GameState.Paused || state == GameState.Reading || state == GameState.AreYouThere)
		//{
			Cursor.lockState = CursorLockMode.Confined;
		//}

		// Player is only moving around during normal play
		if(PlayerControl.InstanceExists())
		{
			PlayerControl.Instance.enabled = (state == GameState.Active);
		}
	}

	/*private IEnumerator ChangeLoadedMenuRoom(GameObject asset, bool destroyPrevLoaded = true, bool centerOnPlayer = true)
	{
		GameObject prevLoaded = currMenuRoomLoaded;
		if(asset == null)
		{
			currMenuRoomLoaded = null;
		}
		else
		{
			AsyncInstantiateOperation inst = InstantiateAsync<GameObject>(asset, 1, null, Vector3.zero, Quaternion.identity);
			while(!inst.isDone)
			{
				yield return null;
			}
			currMenuRoomLoaded = (GameObject) inst.Result[0];
			yield return null;

			if(centerOnPlayer)
			{
				currMenuRoomLoaded.transform.SetPositionAndRotation(PlayerControl.Instance.transform.position, PlayerControl.Instance.transform.rotation);
			}
		}
		
		if(destroyPrevLoaded && prevLoaded != null)
		{
			Destroy(prevLoaded);
			yield return null;
		}
	}*/

	private void ListenForRebindingMenu()
	{
		if(!InputManager.InstanceExists()) { return; }

		InputManager.Instance.OnOpenedRebindingMenuEvent.AddListener((iM) => UpdateState(GameState.RebindingMenu));
		InputManager.Instance.OnClosedRebindingMenuEvent.AddListener((iM) => UpdateState(GameState.Active));
	}

#endregion

#region Transition Fading

	private void SceneTransitionCut(Color toCol, bool enableBlackout)
	{
		transitionBlackout.color = toCol;
		transitionBlackout.gameObject.SetActive(enableBlackout);
	}

	private IEnumerator SceneTransitionFade(float lengthSeconds, Color startCol, Color endCol, bool enableBlackoutAtEnd)
	{
		while(!didStart) { yield return null; }
		//Debug.Log($"Screen Transition Fade from {startCol} to {endCol} in {lengthSeconds} seconds");
		SceneTransitionCut(startCol, true);

		float timeStarted = Time.realtimeSinceStartup;
		float timeElapsed = 0.0f;
		lengthSeconds = Mathf.Max(lengthSeconds, 0.1f);

		do // do-while is just while, but run the code first, then check condition
		{
			transitionBlackout.color = Color.Lerp(startCol, endCol, timeElapsed / lengthSeconds);

			timeElapsed = Mathf.Abs(Time.realtimeSinceStartup - timeStarted);
			yield return null;
		}
		while(timeElapsed < lengthSeconds);

		SceneTransitionCut(endCol, enableBlackoutAtEnd);
	}

	private IEnumerator FadeToBlack(float lengthSeconds)
		=> SceneTransitionFade(lengthSeconds, noColour, sceneTransitionBackgroundColour, true);

	private IEnumerator FadeFromBlack(float lengthSeconds)
		=> SceneTransitionFade(lengthSeconds, sceneTransitionBackgroundColour, noColour, false);

#endregion

#region Timeout

	private void CheckTimeout()
	{
		// Can only timeout if usual operation, and if the timeout functionality is not disabled
		if(state != GameState.Active || timeoutDisabled)
		{
			timeoutTimer = 0.0f; // Reset timer
			return;
		}

		// Periodically check visible plaques
		currSecsBetweenCountVisiblePlaques -= Time.deltaTime;
		if(currSecsBetweenCountVisiblePlaques <= 0.0f)
		{
			currSecsBetweenCountVisiblePlaques += secsBetweenCountVisiblePlaques;
			CountNumVisiblePlaques();
		}
		// If at least one plaque visible, don't timeout
		if(numPlaquesVisible > 0)
		{
			timeoutTimer = 0.0f; // Reset timer
			return;
		}

		// Update time since last input
		timeoutTimer += Time.deltaTime;

		if(timeoutTimer < timeoutWaitSecs)
		{
			// Hasn't been long enough for a timeout
			return;
		}

		// TIMEOUT
		// -------
		Debug.Log("TIMEOUT OCCURRED");
		UpdateState(GameState.AreYouThere);
	}

	private void SetListenersForInput()
	{
		if(inputsThatResetTimeoutTimer.Length == 0)
		{
			Debug.LogWarning($"Warning: There are no inputs that can reset the timeout, timeout feature will be disabled!");
			timeoutDisabled = true;
			return;
		}

		// Set all of the listeners
		foreach(InputActionReference act in inputsThatResetTimeoutTimer)
		{
			act.action.started += ((ctx) => ResetTimeoutTimer());
		}
	}

	private void ResetTimeoutTimer()
	{
		// Timeout timer can only be reset in normal play
		if(state != GameState.Active)
		{
			return;
		}

		// Reset timer
		timeoutTimer = 0.0f;
	}

	private void ScanForPlaques()
	{
		if(timeoutDisabled) { return; }

		// Get all plaques in the scene
		plaquesFoundInScene = GameObject.FindObjectsByType<Plaque>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		
		// If the array is null, just set it to an empty array.
		plaquesFoundInScene ??= new Plaque[0];
	}

	/// <summary>
	/// Out of all the <see cref="Plaque"/>s found in the scene, how many of them are open? Stores the result in <c>numPlaquesVisible</c>.
	/// Note that if no plaques were found in the scene, <c>numPlaquesVisible</c> will be <c>0</c>.
	/// </summary>
	private void CountNumVisiblePlaques()
	{
		if(timeoutDisabled) { return; }

		numPlaquesVisible = 0;
		foreach(Plaque p in plaquesFoundInScene)
		{
			if(p.IsVisible())
			{
				numPlaquesVisible++;
			}
		}
	}

	public void TimeoutPromptDismiss()
	{
		if(state != GameState.AreYouThere)
		{
			Debug.LogWarning($"Warning: Timeout prompt was somehow dismissed in wrong {nameof(GameState)}! (is {state}, should be {nameof(GameState.AreYouThere)})");
			return;
		}

		// Exit timeout prompt
		UpdateState(GameState.Active);
	}

	public void TimeoutPromptRestartScene()
	{
		if(state != GameState.AreYouThere)
		{
			Debug.LogWarning($"Warning: Timeout prompt was somehow confirmed in wrong {nameof(GameState)}! (is {state}, should be {nameof(GameState.AreYouThere)})");
			return;
		}

		// Exit timeout prompt
		UpdateState(GameState.Active);

		// Reset everything
		ResetEverything();
	}

#endregion

#region Security Menu Listening

	private void ListenForSecurityMenus()
	{
		// Get all security menus in the scene
		SecurityMenu[] menus = GameObject.FindObjectsByType<SecurityMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach(SecurityMenu m in menus)
		{
			m.OnOpenedSecurityMenuEvent.AddListener((sM) => SecurityMenuOpened(sM));
			m.OnClosedSecurityMenuEvent.AddListener((sM) => SecurityMenuClosed(sM));
		}
	}

	private void SecurityMenuOpened(SecurityMenu secMenu)
	{
		if(secMenu == null)
		{
			Debug.LogWarning($"Warning: Can't open null {nameof(SecurityMenu)}!");
			return;
		}
		if(currOpenSecurityMenu != null && currOpenSecurityMenu != secMenu)
		{
			Debug.LogWarning($"Warning: {nameof(SecurityMenu)} already open ({currOpenSecurityMenu}), closing it to open {secMenu}!");
			currOpenSecurityMenu.CancelAndExitMenu();
		}

		currOpenSecurityMenu = secMenu;
		UpdateState(GameState.SecurityMenu);
	}

	private void SecurityMenuClosed(SecurityMenu secMenu)
	{
		if(secMenu == null)
		{
			Debug.LogWarning($"Warning: Can't close null {nameof(SecurityMenu)}!");
			return;
		}
		currOpenSecurityMenu = null;
		UpdateState(GameState.Active);
	}

	public bool SecurityMenuCanOpen()
	{
		return (currOpenSecurityMenu == null && state == GameState.Active);
	}

#endregion

	/// <summary>
	/// Quits the application, or stops play mode when in the Unity Editor.
	/// </summary>
	public void QuitGame()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.ExitPlaymode();
#else
		Application.Quit();
#endif
	}
}
