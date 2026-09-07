using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerTests
{
	private GameManager gameManager;
	private GameObject gameManagerObj;
	private GameObject hudObj;
	private GameObject transitionBlackoutObj;
	private GameObject areYouTherePromptObj;

	[SetUp]
	public void Setup()
	{
		// Make sure a previous test has not left the GameManager singleton assigned.
		ClearSingleton();

		// GameManager.Awake() fires the moment the object is active, so we need
		// to wire up the serialised fields first. Create it inactive, assign, then activate later.
		gameManagerObj = new GameObject($"{nameof(GameManager)}_TestObj");
		gameManagerObj.SetActive(false);
		gameManager = gameManagerObj.AddComponent<GameManager>();

		// These just need to exist - GameManager checks for null in InitSingleton
		hudObj = new GameObject("HUD");
		Canvas hud = hudObj.AddComponent<Canvas>();

		transitionBlackoutObj = new GameObject("Transition Blackout");
		Image transitionBlackout = transitionBlackoutObj.AddComponent<Image>();

		areYouTherePromptObj = new GameObject("Are You There Prompt");

		SetPrivateField(gameManager, "hud", hud);
		SetPrivateField(gameManager, "transitionBlackout", transitionBlackout);
		SetPrivateField(gameManager, "areYouTherePromptScreen", areYouTherePromptObj);

		// Activating the object now triggers Awake with the required fields already assigned.
		gameManagerObj.SetActive(true);
	}

	[TearDown]
	public void TearDown()
	{
		// Reset global time scale in case the test changed the GameManager state.
		Time.timeScale = 1.0f;

		Object.DestroyImmediate(gameManagerObj);
		Object.DestroyImmediate(hudObj);
		Object.DestroyImmediate(transitionBlackoutObj);
		Object.DestroyImmediate(areYouTherePromptObj);

		ClearSingleton();
	}

	[Test]
	public void SecurityMenuCanOpen_WhenActiveAndNoMenuOpen_ReturnsTrue()
	{
		SetGameState(GameManager.GameState.Active);

		Assert.IsTrue(gameManager.SecurityMenuCanOpen());
	}

	[Test]
	public void SecurityMenuCanOpen_WhenSecurityMenuAlreadyOpen_ReturnsFalse()
	{
		// SecurityMenuCanOpen() just checks currOpenSecurityMenu == null && state == Active,
		// so we don't need to go through SecurityMenuOpened() - just shove a menu in directly
		GameObject securityMenuObj = new GameObject("Security Menu");
		securityMenuObj.SetActive(false);
		SecurityMenu securityMenu = securityMenuObj.AddComponent<SecurityMenu>();

		SetGameState(GameManager.GameState.Active);
		SetPrivateField(gameManager, "currOpenSecurityMenu", securityMenu);

		Assert.IsFalse(gameManager.SecurityMenuCanOpen());

		Object.DestroyImmediate(securityMenuObj);
	}

	// Helpers

	private void SetGameState(GameManager.GameState state)
	{
		// UpdateState is private, so reflection is used to place GameManager into the state being tested.
		MethodInfo updateState = typeof(GameManager).GetMethod("UpdateState", BindingFlags.Instance | BindingFlags.NonPublic);
		updateState.Invoke(gameManager, new object[] { state });
	}

	private void SetPrivateField(object target, string fieldName, object value)
	{
		// Allows the test to assign serialised private fields without using a real Unity scene.
		FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		field.SetValue(target, value);
	}

	private void ClearSingleton()
	{
		// GameManager uses a private static singleton, so reset it between tests.
		FieldInfo instanceField = typeof(GameManager).GetField("instance", BindingFlags.Static | BindingFlags.NonPublic);
		instanceField.SetValue(null, null);
	}
}