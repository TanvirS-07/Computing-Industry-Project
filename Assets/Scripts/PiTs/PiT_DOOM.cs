using System.Collections;
using UnityEngine;

/// <summary>
/// Project Plan (v1.0 onwards) description:
/// DOOM (1993): The most recognised stepping-stone on the transition from 2D to 3D. Includes graphical
///   tricks to get around hardware limitations, such as flat images always facing the camera (billboarding).
///   DOOM furthered the "Do video games cause violence" debate after the Columbine High School massacre in 1999.
/// </summary>
[DisallowMultipleComponent]
public class PiT_DOOM : PiT
{
	[Header(nameof(PiT_DOOM)+" Settings")]
	[SerializeField] private PiT_DOOM_EightBillboard eightSidedBillboard;


	// Awake() is called the moment the object is active in the scene
	protected override void Awake()
	{
		base.Awake();

		if(eightSidedBillboard == null)
		{  Debug.LogError($"Error: \"{gameObject.name}\" ({nameof(PiT_DOOM)}) has no {nameof(eightSidedBillboard)} assigned!");  }
	}

	public override void Load()
	{
		base.Load();

		eightSidedBillboard.enabled = true; // Enable script
		eightSidedBillboard.Spinning = true; // Default to spinning
	}

	public override void Unload()
	{
		base.Unload();

		eightSidedBillboard.enabled = false; // Disable script
	}

	public void ToggleSpinning()
	{
		// Toggle spinning
		eightSidedBillboard.Spinning = !eightSidedBillboard.Spinning;
	}
}
