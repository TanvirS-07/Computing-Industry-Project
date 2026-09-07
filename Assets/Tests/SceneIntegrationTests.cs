using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEditor;
using System.Linq;
using System.Reflection;




/// <summary>
/// Integration tests for the entire scene
/// </summary>
public class SceneIntegrationTests
{
	Scene gameScene;

	List<int> persistentSceneObjsInstanceIDs = new();

	[UnityOneTimeSetUp]
	public IEnumerator UnityOneTimeSetUp()
	{
		// Load default game scene
		AsyncOperation aO = SceneManager.LoadSceneAsync("MainScene", LoadSceneMode.Additive);
		// Wait until the scene has finished loading
		while(!aO.isDone)
		{  yield return null;  } // wait 1 frame

		gameScene = SceneManager.GetSceneByName("MainScene");

		aO = SceneManager.UnloadSceneAsync(gameScene.name, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
		// Wait until the scene has finished unloading
		while(!aO.isDone || gameScene.isLoaded)
		{  yield return null;  } // wait 1 frame

		Debug.Log(gameScene.IsValid());
		Debug.Log(gameScene.isLoaded);
		Debug.Log(gameScene.name);
		GameObject[] persistentRootsInScene = gameScene.GetRootGameObjects();
		foreach(GameObject root in persistentRootsInScene)
		{
			foreach(Transform obj in root.GetComponentsInChildren<Transform>(true)) // root and its descendants (includes RectTransforms)
			{
				persistentSceneObjsInstanceIDs.Add(obj.GetInstanceID());
			}
		}
	}

	//[OneTimeTearDown]
	//public void OneTimeTearDown()
	//{
	//	
	//}

	private bool IsPersistentInGameScene(GameObject obj)
	{
		return persistentSceneObjsInstanceIDs.Contains(obj.GetInstanceID());
	}

	[UnitySetUp]
	public IEnumerator UnitySetUp()
	{
		// Load default game scene
		AsyncOperation aO = SceneManager.LoadSceneAsync(gameScene.name, LoadSceneMode.Additive);
		// Wait until the scene has finished loading
		while(!aO.isDone || !gameScene.isLoaded)
		{  yield return null;  } // wait 1 frame

		Debug.Log($"Loaded Scene: {gameScene.name}");
	}

	[UnityTearDown]
	public IEnumerator UnityTearDown()
	{
		AsyncOperation aO = SceneManager.UnloadSceneAsync(gameScene.name, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
		// Wait until the scene has finished unloading
		while(!aO.isDone || gameScene.isLoaded)
		{  yield return null;  } // wait 1 frame

		Debug.Log($"Unloaded Scene: {gameScene.name}");
	}

#if UNITY_EDITOR
	/// <summary>
	/// THIS TEST IS EDITOR-ONLY, IT CANNOT BE RUN IN THE PLAYER DUE TO REQUIRING <see cref="EditorUtility"/> and <see cref="PrefabUtility"/> CLASSES.
	/// </summary>
	[UnityTest]
	public IEnumerator GameManager_WhenRestartingScene_HierachyAndNamesIdenticalToStartup()
	{
		// Wait until the GameManager has performed start
		while(!GameManager.Instance.didStart)
		{  yield return null;  } // wait 1 frame

		yield return null; // wait 1 extra frame as buffer

		// Wait until the GameManager has entered active state
		while(GameManager.Instance.CurrentGameState != GameManager.GameState.Active)
		{  yield return null;  } // wait 1 frame


		yield return null; // wait 1 extra frame as buffer


		// ==========================================================================
		// Record all instance IDs of every object in the scene to compare with later
		// ==========================================================================
		GameObject[] existingRootsOnStartup = GameManager.Instance.gameObject.scene.GetRootGameObjects();
		List<GameObject> existingGameObjectsOnStartup = new();
		List<string> existingGameObjectsOnStartupNames = new();
		Dictionary<int, int> existingInstanceIDsOnStartupParentIDs = new();
		Dictionary<int, GameObject> existingInstPrefabsOnStartup = new();
		// instance IDs can never (naturally) be zero, so we'll use it to mean objects that don't exist
		const int ROOT_OBJ_PARENT_INSTANCE_ID = 0;

		foreach(GameObject root in existingRootsOnStartup)
		{
			foreach(Transform obj in root.GetComponentsInChildren<Transform>(true)) // root and its descendants (includes RectTransforms)
			{
				existingGameObjectsOnStartup.Add(obj.gameObject);
				existingGameObjectsOnStartupNames.Add(obj.name);
			}
		}
		for(int i = 0; i < existingGameObjectsOnStartup.Count; i++)
		{
			GameObject obj = existingGameObjectsOnStartup[i];

			if(IsPersistentInGameScene(obj))
			{
				existingInstanceIDsOnStartupParentIDs.Add(
					obj.GetInstanceID(),
					(obj.transform.parent == null) ? ROOT_OBJ_PARENT_INSTANCE_ID : obj.transform.parent.GetInstanceID()
				);
			}
			else
			{
				// This object might bea prefab e.g. PiT, in which case it won't have the same instanceID.
				// We'll compare via the corresponding object in the prefab asset.
				GameObject correspondingObj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj);
				existingInstPrefabsOnStartup.Add(i, correspondingObj);
			}
		}


		yield return null; // wait 1 extra frame as buffer


		// ===========================
		// Forcefully reload the scene
		// ===========================
		IEnumerator iEn = (IEnumerator) GameManager.Instance.GetType().GetRuntimeMethods()
			.First((MethodInfo f) => f.Name == "ReloadWorld")
			.Invoke(GameManager.Instance, new object[0]);

		yield return GameManager.Instance.StartCoroutine(iEn);

		// We should be in active state
		Assert.That(GameManager.Instance.CurrentGameState == GameManager.GameState.Active);


		yield return null; // wait 1 extra frame as buffer


		// ===================================================================================
		// RE-RECORD all instance IDs of every object in the scene and compare against startup
		// ===================================================================================
		GameObject[] currentExistingRoots = GameManager.Instance.gameObject.scene.GetRootGameObjects();
		List<GameObject> currentExistingGameObjects = new();
		foreach(GameObject root in currentExistingRoots)
		{
			foreach(Transform obj in root.GetComponentsInChildren<Transform>(true)) // root and its descendants
			{  currentExistingGameObjects.Add(obj.gameObject);  }
		}

		// Objects that existed on startup should still exist
		// --------------------------------------------------
		for(int i = 0; i < currentExistingGameObjects.Count; i++)
		{
			GameObject obj = currentExistingGameObjects[i];

			if(IsPersistentInGameScene(obj))
			{
				Debug.Log("PERSISTENT: "+obj);
				// It isn't from a prefab, so it should have always existed.
				if(obj == null || !currentExistingGameObjects.Contains(obj))
				{
					Assert.Fail($"\"{existingGameObjectsOnStartupNames[i]}\" existed on startup, but now doesn't.");
				}
			}
			else
			{
				Debug.Log("NOT: "+obj);
				// Since it is not originally from the scene, the Instance ID won't be the same.
				// Compare via the corresponding object inside the prefab asset.
				GameObject currCorrespondingObj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj);
				GameObject startupCorrespondingObj = existingInstPrefabsOnStartup[i];

				if(startupCorrespondingObj.GetInstanceID() != currCorrespondingObj.GetInstanceID())
				{
					Assert.Fail($"Prefab mismatch before and after scene restart: Before {existingGameObjectsOnStartup[i]}, After {obj}.");
				}
			}
		}

		// Objects that were created AFTER startup shouldn't exist anymore
		// ---------------------------------------------------------------
		for(int i = 0; i < currentExistingGameObjects.Count; i++)
		{
			GameObject obj = currentExistingGameObjects[i];

			if(IsPersistentInGameScene(obj))
			{
				if(!existingGameObjectsOnStartup.Contains(obj) || !existingInstanceIDsOnStartupParentIDs.ContainsKey(obj.GetInstanceID()))
				{
					Assert.Fail($"\"{obj}\" still exists, but didn't on startup.");
					continue;
				}

				int originalParentInstanceID = existingInstanceIDsOnStartupParentIDs[obj.GetInstanceID()];

				// Check if this gameobject had NO parent on startup as it should now
				if(obj.transform.parent == null && originalParentInstanceID != ROOT_OBJ_PARENT_INSTANCE_ID)
				{
					Assert.Fail($"\"{obj}\" was a root object on startup, but now isn't! Current parent: \"({obj.transform.parent})\".");
				}

				// Check if this gameobject had the same parent on startup as it should now
				if(obj.transform.parent != null && originalParentInstanceID != obj.transform.parent.GetInstanceID())
				{
					object originalParent = EditorUtility.EntityIdToObject(originalParentInstanceID);
					Assert.Fail(
						$"\"{obj}\" had a different parent on startup! Original parent (Instance ID \"{originalParentInstanceID}\"): \"{((originalParent == null) ? "missing" : originalParent)}\""
						+ $", Current parent (Instance ID \"{obj.transform.parent.GetInstanceID()}\"): \"{obj.transform.parent}\".");
				}
			}
			else
			{
				// Technically already checked for in the previous for loop

				/*// Since it is from a prefab, the Instance ID won't be the same.
				// Compare via the corresponding object inside the prefab asset.
				GameObject currCorrespondingObj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj);
				GameObject startupCorrespondingObj = existingInstPrefabsOnStartup[i];

				if(startupCorrespondingObj.GetInstanceID() != currCorrespondingObj.GetInstanceID())
				{
					Assert.Fail($"Prefab mismatch before and after scene restart: Before {existingGameObjectsOnStartup[i]}, After {obj}.");
				}*/
			}
		}
	}
#endif
}
