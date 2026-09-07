using System;
using TMPro;
using UnityEngine;

public class CenterTextBoard : MonoBehaviour
{
	public enum CenterTextBoardShowType
	{
		HIDE,
		CONSOLETOFILE,
		CUSTOM,
	}

#region Singleton
	private static CenterTextBoard instance;
	[HideInInspector] public static CenterTextBoard Instance
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
			Debug.Log($"Note: \"{nameof(CenterTextBoard)}\" instance exists, destroying new one!");
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
		GetBoardShowType();
		// Only hide the board if in HIDE type
		textBoardVisualRoot.SetActive(showType != CenterTextBoardShowType.HIDE);
		textBoard.enabled = (showType != CenterTextBoardShowType.HIDE);
	}
#endregion

	[SerializeField] private string CENTER_TEXT_BOARD_SETTINGS_FILE_NAME = "CenterTextBoardSettings.txt";
	[SerializeField] private CenterTextBoardShowType showType;
	[HideInInspector] public CenterTextBoardShowType ShowType { get => showType; }
	[SerializeField] private TMP_Text textBoard;
	[HideInInspector] public TMP_Text TextBoard { get => textBoard; }
	[SerializeField] private GameObject textBoardVisualRoot;

	private void GetBoardShowType()
	{
		// Default to hiding the board
		showType = CenterTextBoardShowType.HIDE;

		if(!FileManager.InstanceExists()) {  return;  }

		// If the FileManager exists, check for the file.
		string fileContents = FileManager.Instance.ReadFile(FileManager.KEYBIND_FILE_PATH, CENTER_TEXT_BOARD_SETTINGS_FILE_NAME);
		Debug.Log("fileContents: " + fileContents);
		// If the file doesn't exist, then leave.
		if(fileContents == null) {  return;  }

		// Try to see if the file contains a show type.
		bool suceeded = Enum.TryParse<CenterTextBoardShowType>(fileContents, ignoreCase: true, out showType);
		if(suceeded)
		{
			Debug.Log($"Detected Center Text Board's type as: \"{showType}\".");
		}
		else
		{
			// If it doesn't, we will assume it is custom text.
			Debug.Log($"Couldn't determine what setting the center text board is from the contents of \"{CENTER_TEXT_BOARD_SETTINGS_FILE_NAME}\", assuming it is custom board text!");
			showType = CenterTextBoardShowType.CUSTOM;
		}

		// If we're in custom mode, the file contents IS the custom text.
		if(showType == CenterTextBoardShowType.CUSTOM)
		{
			textBoard.text = fileContents;
		}
	}
}
