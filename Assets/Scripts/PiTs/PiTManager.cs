using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class PiTManager : MonoBehaviour
{
#region Constant Settings

	// These are the settings that have to be changed here
	// ===================================================
	public const string PIT_FILEPATH = "Prefabs/PiTs/"; // From inside the Assets/Resources folder
	public const bool DEBUG_SHOW_SNAP_POINTS = true;
	public const bool DEBUG_WARN_IF_PIT_INCORRECT = true;

#endregion

#region Asset Loading

	private static PiT[] allPiTs;

	public static PiT GetPiTAsset(int index)
	{
		if(index < 0 || index >= allPiTs.Length)
		{
			Debug.LogWarning($"Warning: index {index} out of range, clamping!");
			index = Mathf.Clamp(index, 0, allPiTs.Length);
		}
		return allPiTs[index];
	}

	// https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void GetAllAssetsResources()
	{
		allPiTs =
			// Load all level rooms
			Resources.LoadAll<PiT>(PIT_FILEPATH)
			// Sort them by their intended index
			.OrderBy((p) => p.LoadOrderIndex).ToArray();


		// Console logging loaded resources
		// ================================

		// Log the number of PiTs loaded, and a list of all their names
		Debug.Log(
			$"{allPiTs.Length} {nameof(PiT)}s loaded: "
				+ string.Join(", ", allPiTs.Select((p) => p.name))
		);

		// Compare the number of PiTs against the number of unique load order indexes.
		// List.Distinct() removes duplicates, so if there are duplicates, then the
		//   list size changes, and thus is now a different size to the original.
		if(allPiTs.Length != allPiTs.ToList().Select((p) => p.LoadOrderIndex).Distinct().Count())
		{
			Debug.LogWarning($"Warning: Two or more {nameof(PiT)}s share a {nameof(PiT.LoadOrderIndex)}!");
		}

		// Possibly check for incorrect rooms
		// ==================================
#if UNITY_EDITOR
#pragma warning disable // For unreachable code (when constant == false)
		if(DEBUG_WARN_IF_PIT_INCORRECT && Debug.isDebugBuild)
		{
			Debug.Log($"Checking all {nameof(PiT)}s for problems... (To disable, set {nameof(DEBUG_WARN_IF_PIT_INCORRECT)} in {nameof(PiTManager)}.cs or {nameof(Debug)}.{nameof(Debug.isDebugBuild)} to false)");
			foreach(PiT p in allPiTs)
			{
				if(p.RoomSnapPoint == null) { Debug.LogError($"Error: {nameof(PiT)} \"{p.name}\" must have its {nameof(p.RoomSnapPoint)} assigned!"); }
				
				// check for duplicate objects that there should be only one of
				Light[] lights = p.GetComponentsInChildren<Light>(true);
				if(lights.Length > 0)
				{
					Debug.Log($"\"{p.name}\" contains {lights.Length} {nameof(Light)}(s)... checking if any are {nameof(LightType.Directional)}...");
					if(lights.Where((Light l) => l.type == LightType.Directional).Count() > 0)
					{
						Debug.LogWarning($"\"{p.name}\" contains more than zero {nameof(LightType.Directional)} {nameof(Light)}s!");
					}
				}
				if(p.GetComponentsInChildren<AudioListener>(true).Length > 0) { Debug.LogWarning($"\"{p.name}\" contains more than zero {nameof(AudioListener)}s!"); }
				if(p.GetComponentsInChildren<Camera>(true).Length > 0) { Debug.LogWarning($"\"{p.name}\" contains more than zero {nameof(Camera)}s!"); }
			}
		}
#pragma warning restore
#endif
	}
#endregion

#region Singleton
	private static PiTManager instance;
	[HideInInspector] public static PiTManager Instance
	{
		get => instance;
		private set
		{
			// If the singleton doesn't already have an instance (or we can replace it)
			if(instance == null)
			{
				instance = value;
				instance.InitSingleton();
				return;
			}
			// If we are the instance
			if(instance == value)
			{
				return;
			}
			Debug.Log($"Note: \"{nameof(PiTManager)}\" instance exists, destroying new one!");
			// Destroy the new component instance (the script).
			Destroy(value);
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

	}
#endregion

	[Header("There are settings inside the script that\n"
			+ "Unity won't show in the inspector, they\n"
			+ "are at the top of \""+nameof(PiTManager)+".cs\"")]
	[Space(15)]
	// These settings are set in the inspector, DON'T change them here
	// ===============================================================
	[Tooltip("The Strategy Pattern that provides the list of placements of each " +nameof(PiT)+"'s " + nameof(PiT.RoomSnapPoint) + " in world space.")]
	[SerializeField] private PiTStrategy placementStrategy;
	[Space(10)]
	[Header("NOT settings, just to expose some internal stuff")]
	[SerializeField] private List<PiT> instantiatedPiTs = new();

	// For placing the PiT into the scene
	private InstantiateParameters roomInstantiateParams;
	private int numPiTsLoading = 0;
	private int numPiTsFinishedLoading = 0;

	public IEnumerator PlaceAllPiTs(Scene sceneToPlaceIn)
	{
		Debug.Log($"Loading all {nameof(PiT)}s...");
		
		if(placementStrategy == null)
		{
			Debug.LogError($"Error: {nameof(PiTManager)} has no {nameof(PiTStrategy)} assigned!");
			yield break; // Stop entire IEnumerator Coroutine here
		}

		DisposeOfPiTs(); // Ensure there are no loaded PiTs

		roomInstantiateParams = new InstantiateParameters()
		{
			parent = null,
			scene = sceneToPlaceIn,
			worldSpace = true,
		};

		// reset counters
		numPiTsLoading = 0;
		numPiTsFinishedLoading = 0;

		placementStrategy.Reset(); // start the placement list from the beginning

		// Load all the PiTs
		foreach(PiT p in allPiTs)
		{
			if(!p.IncludeInLoad) { continue; } // Skip PiT

			if(!placementStrategy.MoveNext()) // If failed to move to next element
			{
				Debug.LogError($"Error: Failed to get {nameof(SnapPoint)} number {numPiTsLoading+1} from {nameof(PiTStrategy)} \"{placementStrategy.GetType().Name}\".");
				break; // break out of this loop (stop loading more PiTs)
			}

			numPiTsLoading++;
			StartCoroutine(InstantiatePiT(p, placementStrategy.Current));
		}

		// Wait until all PiTs have loaded
		while(numPiTsFinishedLoading < numPiTsLoading)
		{
			yield return null; // Come back next frame
		}
		
		// Reactivate all the PiTs
		foreach(PiT p in instantiatedPiTs)
		{
			p.gameObject.SetActive(true);
		}

		Debug.Log($"Finished loading all {nameof(PiT)}s");
	}

	private IEnumerator InstantiatePiT(PiT roomAsset, SnapPoint snapPoint)
	{
		Debug.Log($"Loading {nameof(PiT)} asset \"{roomAsset.name}\"");

		// Instantiate Asset
		// -----------------
		// Temporarily deactivate the ASSET because we want the instantiated version to be deactivated
		// This is because the PiT has had at least 1 frame to run functions like Awake(), Start(), and Update()
		roomAsset.gameObject.SetActive(false);

		// Instantiate the PiT, cancel if this script is destroyed (which happens when exiting playmode, otherwise the inst can persist)
		AsyncInstantiateOperation<PiT> asyncLoad = InstantiateAsync<PiT>(roomAsset, roomInstantiateParams, this.destroyCancellationToken);
		// Don't continue until instantiated
		while(!asyncLoad.isDone) { yield return null; } // Wait one frame
		PiT instPiT = asyncLoad.Result[0];
		
		// Reactivate the ASSET, otherwise it will be disabled in the editor
		roomAsset.gameObject.SetActive(true);

		// Get rid of the "(Clone)" postfix appended by the instantiat
		instPiT.gameObject.name = roomAsset.gameObject.name;

		// Place Instantiated Asset
		// ------------------------
		// Set rotation FIRST, then position. Ignore the suggested optimisation to ".SetPositionAndRotation()".
		instPiT.transform.rotation = // Rotate the entire PiT such that its snap point has the same world rotation as the target rotation.
			snapPoint.snapRotWorld * Quaternion.Inverse(instPiT.RoomSnapPoint.rotation) * instPiT.transform.rotation;

		// Translate the entire PiT such that the snap point is overlapping the target position.
		instPiT.transform.position += (snapPoint.snapPosWorld - instPiT.RoomSnapPoint.position);


		// Finish Function
		// ---------------
		// Show or hide room snap point visuals
		if(instPiT.RoomSnapPoint.TryGetComponent(out Renderer node))
		{ node.enabled = DEBUG_SHOW_SNAP_POINTS && Debug.isDebugBuild; }

		instantiatedPiTs.Add(instPiT); // Add to the list of instantiated
		numPiTsFinishedLoading++; // flag that another has finished being loaded

		Debug.Log($"Finished loading {nameof(PiT)} asset \"{roomAsset.name}\"");
	}

	public void DisposeOfPiTs()
	{
		Debug.Log("Disposing of PiTs...");

		foreach(PiT p in instantiatedPiTs)
		{
			p.Unload();
			Destroy(p.gameObject);
		}

		instantiatedPiTs = new(); // wipe list
	}

#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		// Draw the strategy's gizmos
		placementStrategy?.DrawGizmos(transform);
	}
#endif
}