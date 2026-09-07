using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class PiTManagerTests
{
	private PiTManager pitManager;
	private PiTStrategy pitStrat;

	[SetUp]
	public void Setup()
	{
		GameObject pitManagerObj = new GameObject($"{nameof(PiTManager)}_TestObj");

		pitManager = pitManagerObj.AddComponent<PiTManager>();

		// Mock the PiT Placement Strategy
		pitStrat = pitManagerObj.AddComponent<PiTStrategy_Mock>();
		pitManager.GetType().GetRuntimeFields()
			.First((FieldInfo f) => f.Name == "placementStrategy")
			.SetValue(pitManager, pitStrat);

#if false
// DEBUG CODE TO DETERMINE WHAT FUNCTION TO USE TO GET/SET THING VIA REFLECTION
// You can find out which of these Get() functions you need by
//   searching the console, and seeing what line it came from
		Type t = pitManager.GetType();
		foreach(var f in t.GetFields(BindingFlags.NonPublic)) { Debug.Log(f); }
		foreach(var f in t.GetMembers(BindingFlags.NonPublic)) { Debug.Log(f); }
		foreach(var f in t.GetProperties(BindingFlags.NonPublic)) { Debug.Log(f); }
		foreach(var f in t.GetDefaultMembers()) { Debug.Log(f); }
		foreach(var f in t.GetNestedTypes(BindingFlags.NonPublic)) { Debug.Log(f); }
		foreach(var f in t.GetRuntimeFields()) { Debug.Log(f); }
		foreach(var f in t.GetRuntimeProperties()) { Debug.Log(f); }
		foreach(var f in t.GetRuntimeMethods()) { Debug.Log(f); }
#endif
	}

	[TearDown]
	public void TearDown()
	{
		foreach(PiT p in UnityEngine.Object.FindObjectsByType<PiT>(FindObjectsSortMode.None))
		{
			UnityEngine.Object.DestroyImmediate(p.gameObject);
		}

		UnityEngine.Object.DestroyImmediate(pitManager.gameObject);
	}

	/// <summary>
	/// Testing Non-Functional Requirement NFR-04 (as of SRS v1.1) "Individual PiTs shall take a maximum of 500 milliseconds to load or unload."
	/// </summary>
	[UnityTest]
	public IEnumerator PiT_WhenLoadingOrUnloading_WithinMaxTime()
	{
		const double pitMaxMillisToLoadOrUnload = 500 / 1000d; // 500 milliseconds converted to seconds

		// Extra setup for this test specifically
		// --------------------------------------
		yield return pitManager.StartCoroutine(pitManager.PlaceAllPiTs(pitManager.gameObject.scene));

		List<PiT> instPits = (List<PiT>) pitManager.GetType().GetRuntimeFields()
			.First((FieldInfo f) => f.Name == "instantiatedPiTs")
			.GetValue(pitManager);

		double start, end;

		foreach(PiT p in instPits)
		{
			// Make sure the PiTs start unloaded
			if(p.IsLoaded)
			{
				p.Unload();
			}

			yield return null; // wait 1 frame

			// Check the amount of time it takes to load
			start = Time.realtimeSinceStartupAsDouble;
			p.Load();
			end = Time.realtimeSinceStartupAsDouble;

			Assert.LessOrEqual((end - start), pitMaxMillisToLoadOrUnload);

			yield return null; // wait 1 frame

			// Check the amount of time it takes to unload
			start = Time.realtimeSinceStartupAsDouble;
			p.Unload();
			end = Time.realtimeSinceStartupAsDouble;

			Assert.LessOrEqual((end - start), pitMaxMillisToLoadOrUnload);
		}

		// Extra teardown for this test specifically
		// -----------------------------------------
		pitManager.DisposeOfPiTs();
	}

	//[Test]
	//public void WriteFile_WhenOverwriteIsTrue_OverwritesExistingFile()
	//{

	//}
}