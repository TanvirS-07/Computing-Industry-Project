using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using System;

public class InputsHUD : MonoBehaviour
{
    private InputSystem_Actions _inputSystemActions;

    [SerializeField] private GameObject _keyboardPanel;
    [SerializeField] private GameObject _controllerPanel;
    [SerializeField] private GameObject _missingPanel;


    [SerializeField] private string textReplaceTarget;
    //i will fix this later but current layout is : WASD->E in order
    [SerializeField] private TMP_Text[] _keyLabels;

    [SerializeField] private TMP_Text _interactPrompt;
    [SerializeField] private GameObject _interactPromptVisualParent;

    [SerializeField] private GameObject[] _buttons;

    [SerializeField] private GamepadButton[] _gamepadButtons;
    public string[] keybindReplacers;

    private bool interactPromptVisible = false;

    private string _interactTemplate;

    private string _currentControlScheme;


    private void Awake()
    {
        _inputSystemActions = new InputSystem_Actions();
        _interactTemplate = _interactPrompt.text;
    }

    //disables all the panels and reveals the correct one based on string check
    public void TogglePanel(string panelName)
    {

        _currentControlScheme = panelName;

        SetBindReplaceStrings();

        _keyboardPanel.SetActive(false);
        _controllerPanel.SetActive(false);
        _missingPanel.SetActive(false);

        switch (panelName)
        {
            case "Keyboard&Mouse":
                InitialiseKeyboardControls();
                _keyboardPanel.SetActive(true);
                break;

            case "Gamepad":
                InitialiseGamepadControls();
                _controllerPanel.SetActive(true);
                break;

            case "Missing":
                _missingPanel.SetActive(true);
                break;

            default:
                break;
        }
    }

    //sets keycap text label to match key in input system
    //(the weird values in the bindings are based on the order in the input actions, for example bindings[3] returns ":Up Arrow" and bindings[0] returns ":Left Stick")
    public void InitialiseKeyboardControls()
    {
        _keyLabels[0].text = InputControlPath.ToHumanReadableString(
                _inputSystemActions.Player.Move.bindings[2].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);

        _keyLabels[1].text = InputControlPath.ToHumanReadableString(
                _inputSystemActions.Player.Move.bindings[6].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);

        _keyLabels[2].text = InputControlPath.ToHumanReadableString(
                _inputSystemActions.Player.Move.bindings[4].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);

        _keyLabels[3].text = InputControlPath.ToHumanReadableString(
                _inputSystemActions.Player.Move.bindings[8].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);

        _keyLabels[4].text = InputControlPath.ToHumanReadableString(
                _inputSystemActions.Player.Interact.bindings[0].effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
    }

    public void InitialiseGamepadControls()
    {
        string interactPath = _inputSystemActions.Player.Interact.bindings.FirstOrDefault(b => b.groups.Contains("Gamepad")).effectivePath;

        if (string.IsNullOrEmpty(interactPath))
        {
            return;
        }

        string bindingName = interactPath.Split('/').Last();

        Sprite interactSprite = GetButtonSprite(bindingName);

        if (interactSprite != null)
        {
            _buttons[0].GetComponent<Image>().sprite = interactSprite;
        }
    }

    public void SetInteractPrompt(bool value)
    {
        string currentString = _interactTemplate;

        string interactKeys = "";

        if (keybindReplacers.Length == 1)
        {
            interactKeys = $"'{keybindReplacers[0]}'";
        }
        else
        {
            interactKeys = string.Join("' or '", keybindReplacers);
            interactKeys = $"'{interactKeys}'";
        }

        currentString = currentString.Replace(
            textReplaceTarget,
            interactKeys);

        _interactPrompt.text = currentString;
        _interactPromptVisualParent.SetActive(value);

        interactPromptVisible = value;
    }

    public void SetBindReplaceStrings()
    {
        if (string.IsNullOrEmpty(_currentControlScheme))
            return;

        var bindings = _inputSystemActions.Player.Interact.bindings;

        keybindReplacers = bindings.Where(b =>
        !string.IsNullOrEmpty(b.groups) &&
        b.groups.Split(';').Contains(_currentControlScheme)).Select(b =>
        InputControlPath.ToHumanReadableString(b.effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice)).ToArray();
    }

    private Sprite GetButtonSprite(string bindingName)
    {
        GamepadButton button = _gamepadButtons.FirstOrDefault(b => b.ButtonStringName == bindingName);

        if (button == null)
        {
            return null;
        }

        bool isXbox = Gamepad.current is DualShockGamepad;

        return button.GetIcon(isXbox);
    }

    public bool InteractPromptIsVisible()
    {
        return interactPromptVisible;
    }

}

[Serializable]
public class GamepadButton
{
    [SerializeField] private string buttonStringName;
    [SerializeField] private Sprite[] buttonIcons;

    public string ButtonStringName => buttonStringName;

    public Sprite GetIcon(bool isXbox)
    {
        if (buttonIcons == null || buttonIcons.Length == 0)
        {
            return null;
        }

        int index = isXbox ? 1 : 0;

        if (index >= buttonIcons.Length)
        {
            return buttonIcons[0];
        }
        return buttonIcons[index];
    }
}