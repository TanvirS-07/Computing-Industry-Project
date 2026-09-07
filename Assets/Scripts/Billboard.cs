using UnityEngine;

/// <summary>
/// A class that rotates a flat object such that it is flat to the screen (it will mimic the camera's XY plane, so they are parallel).
/// </summary>
public class Billboard : MonoBehaviour
{
	[Header(nameof(Billboard)+" Settings")]
	[SerializeField] protected Transform billboardRotPivot;
	[HideInInspector] public Transform BillboardRotPivot { get => billboardRotPivot; }

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	protected virtual void Start()
	{
		if(billboardRotPivot == null)
		{  Debug.LogError($"Error: {this} has no ({nameof(billboardRotPivot)}) assigned in inspector!");  }
	}

	// Update is called once per frame
	protected virtual void Update()
	{
		// Copy the camera's rotation in order to be flat to the screen (both have the same XY plane, so they are parallel).
		// We don't need to then rotate the pivot by 180 degrees as the billboard itself is rotated 180 degrees around the pivot.
		if(PlayerControl.InstanceExists())
		{
			billboardRotPivot.rotation = PlayerControl.Instance.PlayerCamera.transform.rotation;
		}
	}
}
