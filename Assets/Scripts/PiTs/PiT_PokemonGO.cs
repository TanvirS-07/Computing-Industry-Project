using UnityEngine;

/// <summary>
/// Project Plan (v1.0 onwards) description:
/// Pokémon GO (2016): Represents Augmented Reality (AR) and
///   the introduction of location-based, globally scaled gaming.
/// </summary>
[DisallowMultipleComponent]
public class PiT_PokemonGO : PiT
{
	[Header(nameof(PiT_PokemonGO)+" Settings")]
	[Tooltip("This is the phone screen that allows you to see the AR-only things.")]
	[SerializeField] private MeshRenderer phoneScreenAR;
	[Tooltip("This is the phone\'s body, that is transparent when looking through the screen.")]
	[SerializeField] private MeshRenderer phoneBody;
	[Tooltip("Each of these meshes can only be seen through the phone screen.")]
	[SerializeField] private MeshRenderer[] onlySeenInAR = new MeshRenderer[0];


	// Awake() is called the moment the object is active in the scene
	protected override void Awake()
	{
		base.Awake();

		if(phoneScreenAR == null)
		{  Debug.LogError($"Error: \"{gameObject.name}\" ({nameof(PiT_PokemonGO)}) has no {nameof(phoneScreenAR)} assigned!");  }
		if(phoneBody == null)
		{  Debug.LogError($"Error: \"{gameObject.name}\" ({nameof(PiT_PokemonGO)}) has no {nameof(phoneBody)} assigned!");  }
	}

	public override void Load()
	{
		base.Load();

		phoneScreenAR.enabled = true; // Enable rendering
		phoneBody.enabled = true; // Enable rendering
		foreach(MeshRenderer rend in onlySeenInAR)
		{
			rend.enabled = true; // Enable rendering
		}
	}

	public override void Unload()
	{
		base.Unload();

		phoneScreenAR.enabled = false; // Disable rendering
		phoneBody.enabled = false; // Disable rendering
		foreach(MeshRenderer rend in onlySeenInAR)
		{
			rend.enabled = false; // Disable rendering
		}
	}
}
