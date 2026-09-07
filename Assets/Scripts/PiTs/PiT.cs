using System.Linq;
using UnityEngine;

/// <summary>
/// <para>Base PiT class. Each PiT is a separate class that inherits this one, in order to implement per-PiT functionality.</para>
/// </summary>
[DisallowMultipleComponent]
public abstract class PiT : DistanceLoader
{
	[Header(nameof(PiT)+" Settings")]
	[SerializeField] private Transform roomSnapPoint;
	[HideInInspector] public Transform RoomSnapPoint { get => roomSnapPoint; }
	[Space(10)]
	[SerializeField] private bool includeInLoad = true;
	[HideInInspector] public bool IncludeInLoad { get => includeInLoad; }
	[SerializeField] private int loadOrderIndex;
	[HideInInspector] public int LoadOrderIndex { get => loadOrderIndex; }

	[Header("Exposed values, not settings")]
	DistanceLoader[] childDistanceLoaders;

	// Implemented per-PiT
	// -------------------
	public override void Load()
	{
		base.Load();

		// Manually load child distance loaders
		foreach(DistanceLoader dL in childDistanceLoaders)
		{
			dL.Load();
		}
	}
	// DON'T disable this gameobject (e.g. by calling "gameObject.SetActive(false);"). Child gameobjects can be disabled.
	public override void Unload()
	{
		base.Unload();

		// Manually unload child distance loaders
		foreach(DistanceLoader dL in childDistanceLoaders)
		{
			dL.Unload();
		}
	}


	// Awake() is called the moment the object is active in the scene
	protected virtual void Awake()
	{
		// We'll manually control each distance loader to save on performance
		childDistanceLoaders =
			gameObject.GetComponentsInChildren<DistanceLoader>(true) // Get inactive objects as well
				.Where((DistanceLoader dL) => dL is not PiT) // Don't control other PiTs (also GetComponentsInChildren() will include this object, so remove that too)
					.ToArray();

		foreach(DistanceLoader dL in childDistanceLoaders)
		{
			dL.SetAutoLoadUnloadDistance(false);
		}


		Unload(); // force unload
	}
}
