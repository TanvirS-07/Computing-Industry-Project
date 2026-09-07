using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
#region Singleton
	private static InputManager instance;
	[HideInInspector] public static InputManager Instance
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
			Debug.Log($"Note: \"{nameof(InputManager)}\" instance exists, destroying new one!");
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
		Instance = this;
	}
	/// <summary>
	/// Acts as a stand-in for Start()/Awake(), should only be called by the Singleton.
	/// </summary>
	private void InitSingleton()
	{
		rebindMenuParent.gameObject.SetActive(false);

		InitSliders();
	}
#endregion


	[Header(nameof(InputManager)+" Settings")]
	[Tooltip("The filename to store input overrides.")]
	[SerializeField] private string inputOverridesFileName = "Keybinds.txt";
	[Tooltip("The map of input actions to apply keybind overrides to.")]
	[SerializeField] private InputActionAsset actionMap;
	[Tooltip("The input actions inside the map above that will appear in the rebind menu.")]
	[SerializeField] private InputActionReference[] changeableActions = new InputActionReference[0];
	[Space(10)]
	[SerializeField] private Transform rebindMenuParent;
	[SerializeField] private Transform rebindContentParentTo;
	[SerializeField] private GameObject actionMapListPrefab;
	[SerializeField] private GameObject actionMapListEntryPrefab;
	[SerializeField] private GameObject rebindButtonPrefab;
	[Space(10)]
	[SerializeField] private Slider camRotSensSlider;
	[SerializeField] private Slider camRotSmthSlider;
	[SerializeField] private Slider moveSpdSlider;
	[SerializeField] private Slider moveAccelSlider;
	[SerializeField] private float sliderAdjustExponent = 3;
	private TextMeshProUGUI camRotSensSliderText;
	private TextMeshProUGUI camRotSmthSliderText;
	private TextMeshProUGUI moveSpdSliderText;
	private TextMeshProUGUI moveAccelSliderText;

	[Tooltip("Fires when the rebinding menu opens.")]
	public UnityEvent<InputManager> OnOpenedRebindingMenuEvent = new();
	[Tooltip("Fires when the rebinding menu closes FOR ANY REASON.")]
	public UnityEvent<InputManager> OnClosedRebindingMenuEvent = new();

	[Header("Exposed values, not settings")]
	[SerializeField] private List<GameObject> instRebindMenuLists = new();
	private string inputOverrides;
	private bool instRebindMenuYet = false;


	public void OpenRebindMenu()
	{
		if(!instRebindMenuYet)
		{
			InstantiateRebindMenu();
			instRebindMenuYet = true;
		}

		rebindMenuParent.gameObject.SetActive(true);
		OnOpenedRebindingMenuEvent.Invoke(this);
	}

	public void CloseRebindMenu(bool saveChanges)
	{
		rebindMenuParent.gameObject.SetActive(false);

		if(saveChanges)
		{
			SaveInputOverrides();
		}

		OnClosedRebindingMenuEvent.Invoke(this);
	}

	public void LoadInputOverrides()
	{
		if(!FileManager.InstanceExists())
		{
			Debug.Log($"No {nameof(FileManager)} could be found to load input overrides!");
			return;
		}

		// Read the binding overrides from file
		// ------------------------------------
		inputOverrides = FileManager.Instance.ReadFile(FileManager.KEYBIND_FILE_PATH, inputOverridesFileName);

		if(string.IsNullOrWhiteSpace(inputOverrides) || inputOverrides == "")
		{
			Debug.Log($"No input overrides found inside file (\"{inputOverridesFileName}\").");
			return;
		}

		// Apply the binding overrides
		actionMap.LoadBindingOverridesFromJson(inputOverrides);

		Debug.Log("Applied input overrides");
	}

	public void SaveInputOverrides()
	{
		if(!FileManager.InstanceExists())
		{
			Debug.Log($"No {nameof(FileManager)} could be found to export input overrides!");
			return;
		}

		FileManager.Instance.WriteFile(
			filePath: System.IO.Path.Combine(FileManager.KEYBIND_FILE_PATH, inputOverridesFileName),
			contentsToWrite: actionMap.SaveBindingOverridesAsJson(),
			overwrite: true
		);

		Debug.Log("Keybind overrides exported to file.");
	}

	public void ClearInputOverrides()
	{
		if(!FileManager.InstanceExists())
		{
			Debug.Log($"No {nameof(FileManager)} could be found to clear input overrides!");
			return;
		}

		actionMap.RemoveAllBindingOverrides();

		// Wipe the file storing the input overrides
		FileManager.Instance.WriteFile(
			filePath: System.IO.Path.Combine(FileManager.KEYBIND_FILE_PATH, inputOverridesFileName),
			contentsToWrite: "",
			overwrite: true
		);

		Debug.Log("Cleared keybind overrides.");

		// Reinstantiate the menu to refresh the labels on the buttons
		InstantiateRebindMenu();
	}

	private void InitSliders()
	{
		camRotSensSliderText = camRotSensSlider.GetComponentInChildren<TextMeshProUGUI>();
		camRotSmthSliderText = camRotSmthSlider.GetComponentInChildren<TextMeshProUGUI>();
		moveSpdSliderText = moveSpdSlider.GetComponentInChildren<TextMeshProUGUI>();
		moveAccelSliderText = moveAccelSlider.GetComponentInChildren<TextMeshProUGUI>();

		// Update the slider to match what they are set to in the inspector
		camRotSmthSlider.value = Mathf.Pow(0.001f, 1.0f / sliderAdjustExponent);
		moveAccelSlider.value = Mathf.Pow(0.0001f, 1.0f / sliderAdjustExponent);

		camRotSensSlider.onValueChanged.AddListener((val) => UpdateSpeedValues());
		camRotSmthSlider.onValueChanged.AddListener((val) => UpdateSpeedValues());
		moveAccelSlider.onValueChanged.AddListener((val) => UpdateSpeedValues());
		moveSpdSlider.onValueChanged.AddListener((val) => UpdateSpeedValues());
	}

	private void UpdateSpeedValues()
	{
		if(PlayerControl.InstanceExists())
		{
			PlayerControl.Instance.UpdateSpeedValues(
				moveSpdSlider.value,
				Mathf.Pow(moveAccelSlider.value, sliderAdjustExponent),
				camRotSensSlider.value,
				Mathf.Pow(camRotSmthSlider.value, sliderAdjustExponent)
			);
		}

		camRotSensSliderText.text = $"{camRotSensSlider.value}";
		moveSpdSliderText.text = $"{moveSpdSlider.value}";
		moveAccelSliderText.text = $"{moveAccelSlider.value}";
		camRotSmthSliderText.text = $"{camRotSmthSlider.value}";
	}

#region Instantiate Rebind Menu

	private void InstantiateRebindMenu()
	{
		LoadInputOverrides();

		// If there's already a menu, destroy it
		foreach(GameObject inst in instRebindMenuLists)
		{
			Destroy(inst);
		}
		instRebindMenuLists.Clear();

		foreach(InputAction act in changeableActions)
		{
			InstRebindMenu_InputAction(act, rebindContentParentTo);
		}

		// For each action mapping, create a list of its actions
		/*foreach(InputActionMap map in actionMap.actionMaps)
		{
			InstRebindMenu_InputActionMap(map, rebindContentParentTo);
		}*/
	}

	/*private void InstRebindMenu_InputActionMap(InputActionMap map, Transform parentTo)
	{
		// Create a list
		GameObject instActList = Instantiate(actionMapListPrefab, parentTo);
		instActList.name = $"{nameof(InputActionMap)}_{map.name}";

		// Name the list
		TextMeshProUGUI instActList_Text = instActList.GetComponentInChildren<TextMeshProUGUI>();
		if(instActList_Text == null)
		{  Debug.LogError($"Error: Couldn't find component {nameof(TextMeshProUGUI)} on prefab {actionMapListPrefab}!");  }

		instActList_Text.text = map.name;
		
		// For each action in the mapping, add an entry in the list
		foreach(InputAction act in map.actions)
		{
			InstRebindMenu_InputAction(act, instActList.transform);
		}

		// Remember this piece of the instantiated menu to potentially destroy later
		instRebindMenuLists.Add(instActList);
	}*/

	private void InstRebindMenu_InputAction(InputAction act, Transform parentTo)
	{
		// Create a list entry
		GameObject instAct = Instantiate(actionMapListEntryPrefab, parentTo);
		instAct.name = $"{nameof(InputAction)}_{act.name}";

		// Name the list entry
		TextMeshProUGUI instAct_Text = instAct.GetComponentInChildren<TextMeshProUGUI>();
		if(instAct_Text == null)
		{  Debug.LogError($"Error: Couldn't find component {nameof(TextMeshProUGUI)} on prefab {actionMapListEntryPrefab}!");  }

		instAct_Text.text = act.name;

		// For each binding of the action in the mapping, add a button that can be rebound
		foreach(InputBinding bind in act.bindings)
		{
			InstRebindMenu_InputBinding(bind, act, instAct.transform);
		}
	}

	private void InstRebindMenu_InputBinding(InputBinding bind, InputAction act, Transform parentTo)
	{
		int bindIndex = act.GetBindingIndex(bind);

		// Create a list entry button
		GameObject instButton = Instantiate(rebindButtonPrefab, parentTo);

		// Name the list entry button based on its binding index
		TextMeshProUGUI instButton_Text = instButton.GetComponentInChildren<TextMeshProUGUI>();
		if(instButton_Text == null)
		{  Debug.LogError($"Error: Couldn't find component {nameof(TextMeshProUGUI)} on prefab {rebindButtonPrefab}!");  }

		instButton.name = $"{nameof(InputBinding)}_{bindIndex}";

		// Label the button with the name of the current binding
		Button instButton_Btn = instButton.GetComponentInChildren<Button>();
		if(instButton_Btn == null)
		{  Debug.LogError($"Error: Couldn't find component {nameof(Button)} on prefab {rebindButtonPrefab}!");  }
	
		instButton_Text.text = bind.ToDisplayString();
		// When the button is pressed, perform interactive binding.
		instButton_Btn.onClick.AddListener(() => PerformInteractiveRebinding(act, instButton_Text, bindIndex));
	}

	private void PerformInteractiveRebinding(InputAction act, TextMeshProUGUI textDisplay, int bindingIndex)
	{
		bool prevEnabled = act.enabled;
		string prevText = textDisplay.text;

		act.Disable();

		var rebind = act.PerformInteractiveRebinding(bindingIndex);

		// Dispose the operation on completion.
		rebind.OnComplete(operation =>
		{
			Debug.Log($"Rebound \"{act}\" to \"{operation.selectedControl}\".");

			textDisplay.text = operation.selectedControl.displayName;

			operation.Dispose();

			if(prevEnabled) { act.Enable(); }
		});

		rebind.OnCancel(operation =>
		{
			Debug.Log($"Cancelled rebind \"{act}\" to \"{operation.selectedControl}\".");

			textDisplay.text = prevText;

			operation.Dispose();

			if(prevEnabled) { act.Enable(); }
		});

		// Start the rebind. This will cause the rebind operation to start running in the
		// background listening for input.
		rebind.Start();
		textDisplay.text = "Listening...";
	}

#endregion
}
