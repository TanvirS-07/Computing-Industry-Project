using UnityEngine;
//using UnityEngine.Pool;


/// <summary>
/// Project Plan (v1.0 onwards) description:
/// Representative of the rise of mobile gaming in the mid 2000s, and a
///   shift towards catering to more casual and family-oriented gaming.
///   Australian-made, included at the client’s request.
/// 
/// <para>See also: <seealso cref="PiT_FruitNinja_Fruit"/> for the implementation of the fruit parabola movement.</para>
/// </summary>
[DisallowMultipleComponent]
public class PiT_FruitNinja : PiT
{
/*#region Constant Settings
	// These are the settings that have to be changed here
	// ===================================================
	public const string FRUIT_FILEPATH = "Prefabs/PiTs/PiT_FruitNinja"; // From inside the Assets/Resources folder
#endregion

#region Asset Loading

	private static PiT_FruitNinja_Fruit[] allFruits;

	// https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void GetAllAssetsResources()
	{
		// Load all fruits
		allFruits = Resources.LoadAll<PiT_FruitNinja_Fruit>(FRUIT_FILEPATH);

		// Console logging loaded resources
		// ================================
#if UNITY_EDITOR
		string dbgmsg = $"all {allFruits.Length} {nameof(PiT_FruitNinja)} fruits loaded";
		foreach(PiT_FruitNinja_Fruit f in allFruits)
		{
			dbgmsg += $", \"{f.gameObject.name}\"";
		}
		Debug.Log(dbgmsg);
#endif
	}
#endregion*/

	[Header(nameof(PiT_FruitNinja)+" Settings")]
	[Tooltip("If false, any flying fruits will finish their trajectory when the PiT is unloaded.")]
	[SerializeField] private bool resetFruitsOnUnload = false;
	[Space(5)]
	[Header(nameof(PiT_FruitNinja)+" Launch Values Settings")]
	[Tooltip("The center of the circle that contains every point a fruit can start / end. Represented with yellow gizmo sphere / square.")]
	[SerializeField] private Transform launchBoundsCenterPos;
	[Tooltip("Use a circle or square for launch bounds. The size of the circle is set with the "+nameof(launchBoundsRadius)
		+" field, and the size of the square is set with the "+nameof(launchBoundsWidthHeight)+" field.")]
	[SerializeField] private bool circularLaunchAreaElseSquare;
	[Tooltip("The center of the circle that contains every point a fruit can start / end. Represented with yellow gizmo sphere.")]
	[SerializeField, Min(0.01f)] private float launchBoundsRadius;
	[Tooltip("The dimensions of the square that contains every point a fruit can start / end. Represented with yellow gizmo square.")]
	[SerializeField, Min(0.01f)] private Vector2 launchBoundsWidthHeight;
	[Tooltip("The min and max height a fruit can be when at the peak of its flight. Represented with green"
		+" gizmo line, and a yellow gizmo line from the launch bounds center to the bottom of this range.")]
	[SerializeField] private Vector2 minMaxFruitHeight;
	[Tooltip("The min and max height a fruit can be, for it to be sliced. That is, if the fruit is outside of this"
		+" vertical range, it cannot be sliced. Represented with red gizmo cube (horizontal extents irrelevant).")]
	[SerializeField] private Vector2 minMaxFruitSliceHeight;
	[Tooltip("The min and max amount of seconds it takes for a fruit to do the entire flight. NOTE: If the gravity"
		+" of the fruits seems off, you will need to calculate this range based on the parabola function inside "
		+nameof(PiT_FruitNinja_Fruit)+", which will require taking the second derivative.")]
	[SerializeField] private Vector2 minMaxFruitDurSecs;
	[Tooltip("The min and max speed a fruit will rotate, in degrees per second.")]
	[SerializeField] private Vector2 minMaxFruitRotSpd;
	[Space(10)]
	[Tooltip("The list of references to all fruits that will fly.")]
	[SerializeField] private PiT_FruitNinja_Fruit[] allFruits;

	//[SerializeField] private int initialPoolSize = 5;
	//private ObjectPool<AudioSource> fruitPool;
	//private readonly string pooledItemNamePrefix = "Pooled " + nameof(AudioSource);

#region OnValidate Function
	protected void OnValidate()
	{
		minMaxFruitHeight = minMaxFruitHeight.ValidateAsMinMaxRange();
		minMaxFruitSliceHeight = minMaxFruitSliceHeight.ValidateAsMinMaxRange();
		minMaxFruitDurSecs = minMaxFruitDurSecs.ValidateAsMinMaxRange();
		minMaxFruitRotSpd = minMaxFruitRotSpd.ValidateAsMinMaxRange();
	}
#endregion

	// Awake() is called the moment the object is active in the scene
	protected override void Awake()
	{
		base.Awake();

		if(allFruits == null || allFruits.Length < 1)
		{  Debug.LogError($"Error: {this} has no fruits ({nameof(PiT_FruitNinja_Fruit)}) in inspector!");  }
		if(launchBoundsCenterPos == null)
		{  Debug.LogError($"Error: {this} has no ({nameof(launchBoundsCenterPos)}) assigned in inspector!");  }

		// Set values for all fruits
		foreach(PiT_FruitNinja_Fruit fruit in allFruits)
		{
			fruit.Init(this);
		}
	}

	public override void Load()
	{
		base.Load();

		// Launch all fruits
		foreach(PiT_FruitNinja_Fruit fruit in allFruits)
		{
			fruit.gameObject.SetActive(true);
			LaunchFruit(fruit);
		}
	}

	public override void Unload()
	{
		base.Unload();

		// Halt all fruits
		StopAllCoroutines();

		// If we want to stop all fruits immediately
		if(resetFruitsOnUnload)
		{
			// Immediately stop all flying fruits
			foreach(PiT_FruitNinja_Fruit fruit in allFruits)
			{
				fruit.StopAllCoroutines();
				LaunchFruitEnd(fruit);
			}
		}
	}


/*#region Pooling
	// Taken from https://docs.unity3d.com/6000.2/Documentation/ScriptReference/Pool.ObjectPool_1.html

	private AudioSource CreatePooledItem()
	{
		GameObject obj = new GameObject(pooledItemNamePrefix);
		AudioSource poolItem = obj.AddComponent<AudioSource>();
		poolItem.transform.SetParent(this.transform);
		poolItem.playOnAwake = false;
		poolItem.loop = false;
		poolItem.mute = false;
		return poolItem;
	}

	// Called when an item is returned to the pool using Release
	private void OnReturnedToPool(AudioSource system)
	{
		system.gameObject.SetActive(false);
	}

	// Called when an item is taken from the pool using Get
	private void OnTakeFromPool(AudioSource system)
	{
		system.gameObject.SetActive(true);
		
	}

	// If the pool capacity is reached then any items returned will be destroyed.
	// We can control what the destroy behavior does, here we destroy the GameObject.
	private void OnDestroyPoolObject(AudioSource system)
	{
		Destroy(system.gameObject);
	}
#endregion*/

	private Vector2 centerOffset;
	private Vector3 startPos;
	private Vector3 endPos;
	private float peakHeight;
	private float flightDurationSecs;
	private Vector3 axisOfRot;
	private float rotSpeed;

	public void LaunchFruit(PiT_FruitNinja_Fruit fruit)
	{
		if(fruit == null)
		{
			Debug.LogWarning($"Warning: Couldn't launch {nameof(PiT_FruitNinja_Fruit)} \"{fruit}\" for {nameof(PiT_FruitNinja)} \"{gameObject.name}\" because it is null!");
			return;
		}

		RandomiseLaunchValues(out startPos, out endPos, out peakHeight, out flightDurationSecs, out axisOfRot, out rotSpeed);

		// Launch fruit
		fruit.Launch(startPos, endPos, peakHeight, flightDurationSecs, axisOfRot, rotSpeed);
	}

	private void RandomiseLaunchValues(out Vector3 startPosition, out Vector3 endPosition, out float peakHeightUnits, out float flightDurationSeconds, out Vector3 axisOfRotation, out float rotationSpeedDegPerSec)
	{
		// Randomise the start and end positions to be somewhere inside the launch circle / square
		// ---------------------------------------------------------------------------------------
		// Put the start and end points in the center
		startPosition = endPosition = launchBoundsCenterPos.position;
		if(circularLaunchAreaElseSquare)
		{
			// Randomly offset the start point
			centerOffset = launchBoundsRadius * UnityEngine.Random.insideUnitCircle;
			endPosition.x += centerOffset.x;
			endPosition.z += centerOffset.y; // Vector2 only has x and y, but we want them in Vector3's x and z (horizontal)
			// Randomly offset the end point
			centerOffset = launchBoundsRadius * UnityEngine.Random.insideUnitCircle;
			startPosition.x += centerOffset.x;
			startPosition.z += centerOffset.y; // Vector2 only has x and y, but we want them in Vector3's x and z (horizontal)
		}
		else
		{
			// Randomly offset the start point
			endPosition.x += UnityEngine.Random.Range(-launchBoundsWidthHeight.x, launchBoundsWidthHeight.x);
			endPosition.z += UnityEngine.Random.Range(-launchBoundsWidthHeight.y, launchBoundsWidthHeight.y);
			// Randomly offset the end point
			startPosition.x += UnityEngine.Random.Range(-launchBoundsWidthHeight.x, launchBoundsWidthHeight.x);
			startPosition.z += UnityEngine.Random.Range(-launchBoundsWidthHeight.y, launchBoundsWidthHeight.y);
		}


		// Randomise height
		peakHeightUnits = UnityEngine.Random.Range(minMaxFruitHeight.x, minMaxFruitHeight.y);

		// Randomise duration
		flightDurationSeconds = UnityEngine.Random.Range(minMaxFruitDurSecs.x, minMaxFruitDurSecs.y);

		// Randomise rotation axis
		axisOfRotation = UnityEngine.Random.onUnitSphere;

		// Randomise rotation speed
		rotationSpeedDegPerSec = UnityEngine.Random.Range(minMaxFruitRotSpd.x, minMaxFruitRotSpd.y);
	}

	// When a fruit has finished being launched, it calls this
	public void LaunchFruitEnd(PiT_FruitNinja_Fruit fruit)
	{
		// Make sure it is finished
		//fruit.StopAllCoroutines();

		fruit.transform.position = launchBoundsCenterPos.position;

		// If we're not loaded, don't launch the fruit again.
		if(!isLoaded)
		{
			fruit.gameObject.SetActive(false);
			return;
		}

		// Re-launch the fruit with different values
		LaunchFruit(fruit);
	}


	public void TrySliceFruit()
	{
		// For each fruit ...
		foreach(PiT_FruitNinja_Fruit fruit in allFruits)
		{
			// ... check if it is high enough to be seen by the user
			//   (we don't want to slice it if it can't be seen).
			if(fruit.CurrentHeight < minMaxFruitSliceHeight.x
			|| fruit.CurrentHeight > minMaxFruitSliceHeight.y)
			{
				continue; // Too high or too low, try next fruit.
			}

			// We can see it, try to slice it.
			if(fruit.Slice())
			{
				return; // If successfully sliced, finish.
			}

			// If unsuccessfully sliced, try next fruit.
		}
		// If all fruits unsuccessfully sliced, oh well. Do nothing.
		Debug.Log($"No {nameof(PiT_FruitNinja_Fruit)} sliced.");
	}


#if UNITY_EDITOR // Gizmos don't exist outside of the editor, so just remove from binaries entirely
	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();

		if(launchBoundsCenterPos == null) { return; }

		Gizmos.color = Color.yellow;

		// Draw launch bounds
		if(circularLaunchAreaElseSquare)
		{
			Gizmos.DrawWireSphere(launchBoundsCenterPos.position, launchBoundsRadius);
		}
		else
		{
			Vector3 launchBoundsWireCubeSize = new()
			{
				// rearrange components to draw correctly, the total width/depth of the cube
				//   is double the launch bounds width (like how diameter is double the radius).
				x = launchBoundsWidthHeight.x * 2.0f,
				y = 0.0f, // vertically flat cube
				z = launchBoundsWidthHeight.y * 2.0f,
			};
			Gizmos.DrawWireCube(launchBoundsCenterPos.position, launchBoundsWireCubeSize);
		}

		// Draw line from launch bounds center to min height
		Gizmos.DrawLine(
			launchBoundsCenterPos.position,
			launchBoundsCenterPos.position + (Vector3.up * minMaxFruitHeight.x)
		);

		// Draw line from min height to max height
		Gizmos.color = Color.green;
		Gizmos.DrawLine(
			launchBoundsCenterPos.position + (Vector3.up * minMaxFruitHeight.x),
			launchBoundsCenterPos.position + (Vector3.up * minMaxFruitHeight.y)
		);

		// Draw box for min max slice height
		// ---------------------------------
		Gizmos.color = Color.red;
		// Shift the center of the box up to be at the midpoint of min and max height
		Vector3 sliceWireCubeCenter = launchBoundsCenterPos.position
			+ Vector3.up * ((minMaxFruitSliceHeight.x + minMaxFruitSliceHeight.y) / 2.0f);
		Vector3 sliceHeightWireCubeSize = new()
		{
			// The width/depth of the cube is double the launch bounds radius / size, like how diameter is double the radius.
			x = 2.0f * (circularLaunchAreaElseSquare ? launchBoundsRadius : launchBoundsWidthHeight.x),
			z = 2.0f * (circularLaunchAreaElseSquare ? launchBoundsRadius : launchBoundsWidthHeight.y),

			// Set the height of the box to be the distance from min to max slice height.
			y = (minMaxFruitSliceHeight.y - minMaxFruitSliceHeight.x),
		};
		Gizmos.DrawWireCube(sliceWireCubeCenter, sliceHeightWireCubeSize);
	}
#endif
}
