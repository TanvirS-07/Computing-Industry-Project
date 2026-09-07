using UnityEngine;

/// <summary>
/// Project Plan (v1.0 onwards) description:
/// Space Invaders (1978): Represents the "Golden Age" of arcade video
///   games and the emergence of gaming as a source of entertainment medium.
/// </summary>
[DisallowMultipleComponent]
public class PiT_SpaceInvaders : PiT
{
	[Header(nameof(PiT_SpaceInvaders)+" Settings")]
	[SerializeField] private PiT_SpaceInvaders_Game arcadeGame;
	[SerializeField] private bool playArcadeOnLoadPiT = false;

	// Awake() is called the moment the object is active in the scene
	protected override void Awake()
	{
		base.Awake();

		if(arcadeGame == null)
		{  Debug.LogError($"Error: \"{gameObject.name}\" ({nameof(PiT_SpaceInvaders)}) has no {nameof(arcadeGame)} assigned!");  }
	}

	public override void Load()
	{
		base.Load();

		arcadeGame.enabled = true; // Enable script
		arcadeGame.ResetSimulation(playArcadeOnLoadPiT); // reset the arcade sim
	}

	public override void Unload()
	{
		base.Unload();

		arcadeGame.enabled = false; // Disable script
	}
}
