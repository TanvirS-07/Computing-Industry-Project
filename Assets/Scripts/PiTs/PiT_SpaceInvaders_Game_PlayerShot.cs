using UnityEngine;

public class PiT_SpaceInvaders_Game_PlayerShot : MonoBehaviour
{
	[Header(nameof(PiT_SpaceInvaders_Game_Invader)+" Settings")]
	[SerializeField, Min(0.0f)] private float speed;
	[SerializeField, Min(0.0f)] private float destroyAfter;

	[Header("Exposed values, not settings")]
	public PiT_SpaceInvaders_Game SimRef;

	// Update is called once per frame
	private void Update()
	{
		// Move the shot
		transform.Translate(Vector3.up * (speed * Time.deltaTime), Space.Self);

		// Check to see if any invaders are present
		CheckForInvader();

		// Count down until we destroy self.
		// No need to use seperate variable, we don't need to remember the original value.
		destroyAfter -= Time.deltaTime;
		if(destroyAfter <= 0.0f)
		{
			Destroy(this.gameObject);
		}
	}

	private void CheckForInvader()
	{
		// Find out which cell the shot is in.
		Vector3Int cellPos = SimRef.WorldToInvaderGrid(transform.position);

		// Is there an invader in that cell as well?
		PiT_SpaceInvaders_Game_Invader inv = SimRef.GetInvaderInCell(cellPos.x, cellPos.y);
		if(inv == null)
		{
			// No invader, return.
			return;
		}
		//Debug.Log(cellPos.x +",  "+ cellPos.y +",  "+ SimRef.WorldToInvaderGrid(inv.transform.position));

		// There was an invader, kill it.
		inv.Die();

		// Destroy this bullet.
		Destroy(this.gameObject);
	}
}
