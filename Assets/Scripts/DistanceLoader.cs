using UnityEngine;

/// <summary>
/// <para>Loads and unloads based on the distance to an object.</para>
/// </summary>
public class DistanceLoader : MonoBehaviour
{
	[Header(nameof(DistanceLoader)+" Settings")]
	[SerializeField, Min(0.0f)] private float unloadDistance = 50.0f;
	[HideInInspector] public float UnloadDistance { get => unloadDistance; }
	[SerializeField] protected bool autoLoadUnloadDistance = true;

	[Header("Exposed values, not settings")]
	[SerializeField] protected bool isLoaded;
	[HideInInspector] public bool IsLoaded { get => isLoaded; }
	public void SetAutoLoadUnloadDistance(bool isAutomatic)
	{
		autoLoadUnloadDistance = isAutomatic;
	}

	public virtual void Load()
	{
		isLoaded = true;
	}
	// DON'T disable this gameobject (e.g. by calling "gameObject.SetActive(false);"). Child gameobjects can be disabled.
	public virtual void Unload()
	{
		isLoaded = false;
	}

	// Update is called once per frame
	protected virtual void Update()
	{
		CheckUnloadDistance();
	}

	private float sqrDistToPlayer;
	protected void CheckUnloadDistance()
	{
		if(!autoLoadUnloadDistance) { return; }

		if(!PlayerControl.InstanceExists()) { return; }

		// If in range or out of range

		// Comparing squares of the distances is significantly cheaper (Taking the square
		//   root is WAY more expensive than squaring the other side of the equation).
		sqrDistToPlayer = (transform.position - PlayerControl.Instance.transform.position).sqrMagnitude;
		if(sqrDistToPlayer > (unloadDistance*unloadDistance))
		{
			// If out of range and we are loaded, then unload
			if(isLoaded) { Unload(); }
		}
		else // If in range
		{
			// If in range and we aren't loaded, then load in
			if(!isLoaded) { Load(); }
		}
	}

#if UNITY_EDITOR // Gizmos don't exist outside of the editor, so just remove from binaries entirely
	protected virtual void OnDrawGizmosSelected()
	{
		if(!autoLoadUnloadDistance) { return; }

		// Visualise the unload distance, coloured based on if the PiT is loaded or not.
		Gizmos.color = (isLoaded) ? Color.green : Color.red;
		Gizmos.DrawWireSphere(transform.position, UnloadDistance);
	}
#endif
}
