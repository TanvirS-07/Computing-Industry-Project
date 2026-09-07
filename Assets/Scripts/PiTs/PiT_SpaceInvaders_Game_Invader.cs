using UnityEngine;

public class PiT_SpaceInvaders_Game_Invader : MonoBehaviour
{
	[Header("Exposed values, not settings")]
	public PiT_SpaceInvaders_Game SimRef;
	public bool TellSimOfDeath = true; // If true, the OnDestroy() function will tell the GameRef.
	[SerializeField] private bool alreadyDying = false;


	// This is useful for simulating the invader's death via deleting it in the hierachy during playmode.
	private void OnDestroy()
	{
		if(!alreadyDying)
		{
			Die();
		}
	}

	// Call this to kill the invader
	public void Die()
	{
		if(!TellSimOfDeath || alreadyDying)
		{
			return; // No need to continue
		}

		if(SimRef == null)
		{
			Debug.LogWarning($"Warning: {this} attempted to inform {nameof(SimRef)} ({nameof(PiT_SpaceInvaders_Game)}) of destruction, but reference was null!");
		}
		else
		{
			// Inform simulation
			SimRef.InvaderDead(this);
		}

		// Make sure this function doesn't run multiple times via OnDestroy().
		alreadyDying = true;

		// Mark self for destruction. This will eventually lead to calling OnDestroy().
		Destroy(this.gameObject);
	}
}
