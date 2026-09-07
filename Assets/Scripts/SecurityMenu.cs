using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Reads the staff shortcut from StreamingAssets.
/// </summary>
[DisallowMultipleComponent]
public class SecurityMenu : MonoBehaviour
{
	[Header("File Names")]
	[SerializeField] private string openMenuKeybindFileName = "ExitKeybind.txt";
	[SerializeField] private string passwordFileName = "ExitPassword.txt";

	[Header("Security Menu UI")]
	[SerializeField] private GameObject menuVisuals;
	[SerializeField] private TMP_InputField passwordInput;
	[SerializeField] private TMP_Text feedbackText;

	[Header("Feedback Messages")]
	[SerializeField] private string defaultMessage = "Enter staff password to close the application.";
	[SerializeField] private string incorrectPasswordMessage = "Incorrect password.";

	private bool menuOpen = false;
	private bool requiresCtrl = false;
	private bool keybindLoaded = false;
	private Key openMenuKey = Key.None;

	[Tooltip("Fires when the security menu opens.")]
	public UnityEvent<SecurityMenu> OnOpenedSecurityMenuEvent = new();
	[Tooltip("Fires when the security menu closes FOR ANY REASON, including wrong/successful password, and cancelled.")]
	public UnityEvent<SecurityMenu> OnClosedSecurityMenuEvent = new();
	[Tooltip("Fires when the security menu recieves the correct password and closes.")]
	public UnityEvent<SecurityMenu> OnPassedSecurityMenuEvent = new();

	private void Awake()
	{
		if(menuVisuals != null)
		{
			menuVisuals.SetActive(false);
		}

		LoadKeybind();
	}

	private void Update()
	{
		if(menuOpen)
		{
			if(Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
			{
				CloseMenu();
			}

			return;
		}

		if(!keybindLoaded || Keyboard.current == null)
		{
			return;
		}

		if(IsShortcutPressed())
		{
			OpenSecurityMenu();
		}
	}

	/// <summary>
	/// Reads the shortcut text file and stores the parsed keybind.
	/// </summary>
	private void LoadKeybind()
	{
		if(!FileManager.InstanceExists())
		{
			Debug.Log($"No {nameof(FileManager)} could be found to load keybind!");
			keybindLoaded = false;
			return;
		}

		string keybindText =
			FileManager.Instance.ReadFile(FileManager.KEYBIND_FILE_PATH, openMenuKeybindFileName)
			.Trim();

		if(!ParseKeybind(keybindText))
		{
			Debug.LogError("Invalid keybind: " + keybindText);
			keybindLoaded = false;
			return;
		}

		keybindLoaded = true;
		Debug.Log($"{this} keybind loaded: " + keybindText);
	}

	/// <summary>
	/// Parses the shortcut keybind text and works out which keys need to be pressed.
	/// </summary>
	private bool ParseKeybind(string keybindText)
	{
		requiresCtrl = false;
		openMenuKey = Key.None;

		if(string.IsNullOrWhiteSpace(keybindText))
		{
			return false;
		}

		string[] parts = keybindText.Split('+', StringSplitOptions.RemoveEmptyEntries);

		foreach(string rawPart in parts)
		{
			string part = rawPart.Trim();

			if(part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
				part.Equals("Control", StringComparison.OrdinalIgnoreCase))
			{
				requiresCtrl = true;
			}
			else if(Enum.TryParse(part, true, out Key parsedKey))
			{
				openMenuKey = parsedKey;
			}
			else
			{
				return false;
			}
		}

		return openMenuKey != Key.None;
	}

	/// <summary>
	/// Checks if the shortcut was pressed.
	/// </summary>
	private bool IsShortcutPressed()
	{
		if(openMenuKey == Key.None)
		{
			return false;
		}

		bool ctrlPressed = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;

		if(requiresCtrl && !ctrlPressed)
		{
			return false;
		}

		return Keyboard.current[openMenuKey].wasPressedThisFrame;
	}

	/// <summary>
	/// Opens the password menu, pauses the game, unlocks the cursor, and disables player movement.
	/// </summary>
	private void OpenSecurityMenu()
	{
		if(GameManager.InstanceExists() && !GameManager.Instance.SecurityMenuCanOpen())
		{
			// If the GameManager says we can't open, then don't
			return;
		}

		if(menuVisuals == null)
		{
			Debug.LogError("Menu is not assigned.");
			return;
		}

		menuOpen = true;

		OnOpenedSecurityMenuEvent.Invoke(this);

		menuVisuals.SetActive(true);

		if(passwordInput != null)
		{
			passwordInput.text = "";
			EventSystem.current?.SetSelectedGameObject(passwordInput.gameObject);
			passwordInput.Select();
			passwordInput.ActivateInputField();
		}

		if(feedbackText != null)
		{
			feedbackText.text = defaultMessage;
		}
	}

	/// <summary>
	/// Checks the password and succeeds if it matches the stored password.
	/// </summary>
	public virtual void TryConfirmPassword()
	{
		string correctPassword = GetPassword();
		string typedPassword = passwordInput != null ? passwordInput.text.Trim() : "";

		if(typedPassword == correctPassword.Trim())
		{
			CloseMenu();
			OnPassedSecurityMenuEvent.Invoke(this);
			return;
		}

		if(feedbackText != null)
		{
			feedbackText.text = incorrectPasswordMessage;
		}

		if(passwordInput != null)
		{
			passwordInput.text = "";
			EventSystem.current?.SetSelectedGameObject(passwordInput.gameObject);
			passwordInput.Select();
			passwordInput.ActivateInputField();
		}
	}

	public void CancelAndExitMenu()
	{
		CloseMenu();
	}

	/// <summary>
	/// Gets the password from FileManager.cs
	/// </summary>
	private string GetPassword()
	{
		if(!FileManager.InstanceExists())
		{
			Debug.Log($"No {nameof(FileManager)} could be found to load the password!");
			return null;
		}

		return FileManager.Instance.ReadFile(FileManager.KEYBIND_FILE_PATH, passwordFileName);
	}

	private void CloseMenu()
	{
		menuOpen = false;

		if(menuVisuals != null)
		{
			menuVisuals.SetActive(false);
		}

		OnClosedSecurityMenuEvent.Invoke(this);
	}
}
