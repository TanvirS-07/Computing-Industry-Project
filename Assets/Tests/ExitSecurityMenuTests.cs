using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;

/// <summary>
/// These are unit tests SPECIFICALLY for the SecurityMenu used to exit the application.
/// </summary>
public class ExitSecurityMenuTests : InputTestFixture
{
	private GameObject securityManagerObj;
	private GameObject exitMenuObj;
	private Component exitSecurityMenu;
	private Keyboard keyboard;

	public override void Setup()
	{
		base.Setup();

		keyboard = InputSystem.AddDevice<Keyboard>();

		exitMenuObj = new GameObject("Exit Menu");
		exitMenuObj.SetActive(false);

		securityManagerObj = new GameObject("Security Manager");

		exitSecurityMenu = securityManagerObj.AddComponent<SecurityMenu>();

		SetPrivateField(exitSecurityMenu, "menuVisuals", exitMenuObj);
		SetPrivateField(exitSecurityMenu, "keybindLoaded", true);
		SetPrivateField(exitSecurityMenu, "requiresCtrl", true);
		SetPrivateField(exitSecurityMenu, "openMenuKey", Key.Q);
	}

	public override void TearDown()
	{
		UnityEngine.Object.DestroyImmediate(securityManagerObj);
		UnityEngine.Object.DestroyImmediate(exitMenuObj);

		base.TearDown();
	}

	[UnityTest]
	public IEnumerator OpenExitMenu_WhenKeybindPressed_ShowsMenu()
	{
		Assert.IsFalse(exitMenuObj.activeSelf);

		Press(keyboard.leftCtrlKey);
		Press(keyboard.qKey);

		yield return null;

		Assert.IsTrue(exitMenuObj.activeSelf);
	}

	[UnityTest]
	public IEnumerator CloseExitMenu_WhenEscapePressed_ClosesMenu()
	{
		Assert.IsFalse(exitMenuObj.activeSelf);

		Press(keyboard.leftCtrlKey);
		Press(keyboard.qKey);

		yield return null;

		Assert.IsTrue(exitMenuObj.activeSelf);

		Release(keyboard.leftCtrlKey);
		Release(keyboard.qKey);

		yield return null;

		Press(keyboard.escapeKey);

		yield return null;

		Assert.IsFalse(exitMenuObj.activeSelf);
	}

	[UnityTest]
	public IEnumerator CloseExitMenu_WhenCancelPressed_ClosesMenu()
	{
		Assert.IsFalse(exitMenuObj.activeSelf);

		Press(keyboard.leftCtrlKey);
		Press(keyboard.qKey);

		yield return null;
		
		Assert.IsTrue(exitMenuObj.activeSelf);

		exitSecurityMenu.GetType().GetMethod("CancelAndExitMenu").Invoke(exitSecurityMenu, null);

		yield return null;

		Assert.IsFalse(exitMenuObj.activeSelf);
	}

	[UnityTest]
	public IEnumerator OpenExitMenu_WhenCtrlNotPressed_DoesNotOpenMenu()
	{
		Assert.IsFalse(exitMenuObj.activeSelf);

		Press(keyboard.qKey);

		yield return null;

		Assert.IsFalse(exitMenuObj.activeSelf);
	}

	private void SetPrivateField(object target, string fieldName, object value)
	{
		FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		field.SetValue(target, value);
	}
}
