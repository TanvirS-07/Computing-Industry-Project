using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// <para>See also: <seealso cref="PiT_SpaceInvaders"/></para>
/// </summary>
[DisallowMultipleComponent, RequireComponent(typeof(Grid))]
public class PiT_SpaceInvaders_Game : MonoBehaviour
{
	[Header(nameof(PiT_SpaceInvaders_Game)+" Settings")]
	[SerializeField] private Transform spcInvPlayer;
	[Tooltip("Treated as the initial location of the player, and the center of the total movement area.")]
	[SerializeField] private Transform spcInvPlayerCenter;
	[SerializeField, Min(0.0f)] private float spcInvPlayerSpd;
	[Tooltip("How far the player can move horizontally in either direction. Visually represented via red Gizmo line.")]
	[SerializeField, Min(0.0f)] private float playerMaxHorizontal;
	[Space(10)]
	[SerializeField, Min(0.0f)] private float secsBetweenPlayerShots;
	[SerializeField] private PiT_SpaceInvaders_Game_PlayerShot playerShotPrefab;
	[Space(20)]
	[SerializeField] private Transform invadersMovePivot;
	[SerializeField] private PiT_SpaceInvaders_Game_Invader invaderToDuplicate;
	[Tooltip("When there is only one invader remaining, this is the number of seconds between moving the invaders.")]
	[SerializeField, Min(0.0f)] private float invaderSpdOne;
	[Tooltip("When all invaders are present, this is the number of seconds between moving the invaders.")]
	[SerializeField, Min(0.0f)] private float invaderSpdAll;
	[Tooltip("The invaders are positioned as a grid. This sets how many rows and columns there are. Visually represented via green Gizmo lines.")]
	[SerializeField] private Vector2Int numInvaders;
	[Tooltip("How many rows the invaders can move down. Visually represented via vertical blue Gizmo lines.")]
	[SerializeField, Min(1)] private int numMoveRows = 1;
	[Tooltip("How many columns the invaders can move side-to-side. Visually represented via horizontal blue Gizmo lines.")]
	[SerializeField, Min(1)] private int numMoveCols = 1;
	[Space(10)]
	[SerializeField] private TMP_Text displayText;
	[SerializeField] private string pressPlayText;
	[SerializeField] private string winText;
	[SerializeField] private string loseText;

	[Header("Exposed values, not settings")]
	[SerializeField] private bool resetting = false;
	[SerializeField] private bool simulating = false;
	[Space(10)]
	[SerializeField] private bool playerMoving = false;
	[SerializeField] private bool playerDirLeftElseRight;
	[SerializeField] private Vector2 spcInvPlayerPos;
	[SerializeField] private float timeOfPrevPlayerShoot;
	[Space(10)]
	[SerializeField] private float secsBetweenMove;
	[SerializeField] private float currSecsBetweenInvaderMove;
	[SerializeField] private bool invaderDirLeftElseRight;
	[SerializeField] private int currInvaderCol;
	[SerializeField] private int currInvaderRow;
	[SerializeField] private List<PiT_SpaceInvaders_Game_Invader> instInvaders = new();
	private Grid invaderGrid;
	private int numInvadersTotal = 0;

	// Awake() is called the moment the object is active in the scene
	private void Awake()
	{
		if(spcInvPlayer == null)
		{  Debug.LogError($"Error: {this} has no {nameof(spcInvPlayer)} assigned!");  }
		if(invadersMovePivot == null)
		{  Debug.LogError($"Error: {this} has no {nameof(invadersMovePivot)} assigned!");  }
		if(invaderToDuplicate == null)
		{  Debug.LogError($"Error: {this} has no {nameof(invaderToDuplicate)} assigned!");  }
		if(playerShotPrefab == null)
		{  Debug.LogError($"Error: {this} has no {nameof(playerShotPrefab)} assigned!");  }
		if(displayText == null)
		{  Debug.LogError($"Error: {this} has no {nameof(displayText)} assigned!");  }

		if(invaderGrid == null)
		{
			invaderGrid = GetComponent<Grid>(); // guaranteed because required component
		}

		//ResetSimulation(false);
	}

#region Invader Grid Functions
	Vector3Int tmp;
	// Helper functions adapting the Unity grid
	private Vector3 InvaderGridToWorld(int x, int y)
	{
		tmp.x = x;
		tmp.y = y;
		tmp.z = 0; // No z coordinate, only 2D grid.
		return invaderGrid.CellToWorld(tmp);
	}
	public Vector3Int WorldToInvaderGrid(Vector3 pos)
	{
		// CellToWorld() returns the bottom-left of the cell. Mimic that here.
		pos.x += (invaderGrid.cellSize.x * 0.25f);
		pos.y += (invaderGrid.cellSize.y * 0.25f);
		tmp = invaderGrid.WorldToCell(pos);
		tmp.z = 0; // No z coordinate, only 2D grid.
		return tmp;
	}

	private bool IsInvaderInCol(int colIndex)
	{
		// For each invader...
		foreach(PiT_SpaceInvaders_Game_Invader inv in instInvaders)
		{
			if(inv == null) { continue; }
			// Does its column coordinate match?
			if(WorldToInvaderGrid(inv.transform.position).x == colIndex)
			{
				return true; // At least one invader is in this column
			}
		}
		return false; // No invaders are in this column
	}

	private bool IsInvaderInRow(int rowIndex)
	{
		// For each invader...
		foreach(PiT_SpaceInvaders_Game_Invader inv in instInvaders)
		{
			if(inv == null) { continue; }
			// Does its row coordinate match?
			if(WorldToInvaderGrid(inv.transform.position).y == rowIndex)
			{
				return true; // At least one invader is in this row
			}
		}
		return false; // No invaders are in this row
	}

	/// <returns><see cref="PiT_SpaceInvaders_Game_Invader"/> if present at the <see cref="rowIndex"/> and <see cref="colIndex"/>, else null.</returns>
	public PiT_SpaceInvaders_Game_Invader GetInvaderInCell(int colIndex, int rowIndex)
	{
		// For each invader...
		foreach(PiT_SpaceInvaders_Game_Invader inv in instInvaders)
		{
			if(inv == null) { continue; }
			//Debug.Log(WorldToInvaderGrid(inv.transform.position)+", "+colIndex+", "+rowIndex+", "+(WorldToInvaderGrid(inv.transform.position).x == colIndex) + ",   "+ (WorldToInvaderGrid(inv.transform.position).y == rowIndex));
			// Does its coordinates match? (row index negated as the positive direction is down, unity grid is up)
			if(WorldToInvaderGrid(inv.transform.position).x == colIndex
			&& WorldToInvaderGrid(inv.transform.position).y == rowIndex)
			{
				return inv; // At least one invader is in this row
			}
		}
		return null; // No invader here
	}
#endregion

	// Call to reset the game
	public void ResetSimulation(bool startPlaying)
	{
		if(resetting) { return; }
		resetting = true;
		StartCoroutine(ResetRoutine(startPlaying));
	}
	private IEnumerator ResetRoutine(bool startPlaying)
	{
		resetting = true;
		simulating = false;

		// Destroy all invader child objects
		// ---------------------------------
		foreach(PiT_SpaceInvaders_Game_Invader inv in instInvaders)
		{
			if(inv == null) { continue; }
			inv.TellSimOfDeath = false; // We don't need to be informed that it was destroyed
		}
		// Start at the END of the list as deleting the first index makes it get replaced by what was the second index.
		// Note that Destroy() only marks an object for destruction. It only gets truly destroyed sometime at the end of the frame.
		for(int i = invadersMovePivot.transform.childCount - 1; i >= 0; i--)
		{
			Destroy(invadersMovePivot.GetChild(i).gameObject);
		}

		yield return null; // Wait one frame, a.k.a. wait until the invaders (marked for destruction) are destroyed.

		instInvaders.Clear(); // remove stale references


		// Instantiate all invaders
		// ------------------------
		// Reset position of move pivot
		invadersMovePivot.position = InvaderGridToWorld(0, 0);
		PiT_SpaceInvaders_Game_Invader inst;
		for(int y = 0; y < numInvaders.y; y++)
		{
			// If you want each row of invaders to look different, change the sprite-to-use here

			for(int x = 0; x < numInvaders.x; x++)
			{
				inst = Instantiate<PiT_SpaceInvaders_Game_Invader>(invaderToDuplicate, invadersMovePivot);
				// Place the invader on the grid
				// Negate y to make the grid of invaders extend downwards
				inst.transform.position = InvaderGridToWorld(x, -y);
				inst.SimRef = this;

				instInvaders.Add(inst);
			}
		}
		// Save total number of invaders (alternatively = numInvaders.x * numInvaders.y)
		numInvadersTotal = instInvaders.Count;

		// Reset other settings
		// --------------------
		playerMoving = false;
		spcInvPlayer.localPosition = Vector3.zero;
		timeOfPrevPlayerShoot = Time.time;

		currSecsBetweenInvaderMove = secsBetweenMove = invaderSpdAll;
		currInvaderRow = currInvaderCol = 0;
		invaderDirLeftElseRight = false; // move right initially

		// Finished resetting
		// ------------------
		resetting = false; // Flag that we're done resetting
		if(startPlaying) // possibly start simulating
		{
			simulating = true;
			displayText.text = ""; // clear text
		}
		else
		{
			// tell the player they can initiate the playing of the game
			displayText.text = pressPlayText;
		}
	}


	// Update is called once per frame
	private void Update()
	{
		if(resetting || !simulating)
		{
			return;
		}

		// move player
		if(playerMoving)
		{
			// Get the player pos
			spcInvPlayerPos = spcInvPlayer.localPosition;

			// Move the player by speed, potentially reverse direction
			spcInvPlayerPos.x += spcInvPlayerSpd * (playerDirLeftElseRight ? -1f : 1f) * Time.deltaTime;

			// Clamp the player pos
			spcInvPlayerPos.x = Mathf.Clamp(spcInvPlayerPos.x, -playerMaxHorizontal, playerMaxHorizontal);

			// Set the player pos
			spcInvPlayer.localPosition = spcInvPlayerPos;
		}

		// Move invaders
		currSecsBetweenInvaderMove -= Time.deltaTime;
		if(currSecsBetweenInvaderMove <= 0.0f)
		{
			// reset timer, add the time to account for overshooting (curr could be below zero)
			currSecsBetweenInvaderMove += secsBetweenMove;
			MoveInvaders();
		}
	}

#region Player Interactable Button Inputs
	public void StartMovingPlayerLeft()
	{
		if(!simulating) { return; }

		playerDirLeftElseRight = true;
		playerMoving = true;
	}
	public void StartMovingPlayerRight()
	{
		if(!simulating) { return; }

		playerDirLeftElseRight = false;
		playerMoving = true;
	}
	public void StopMovingPlayer()
	{
		playerMoving = false;
	}
	public void TryPlayerShoot()
	{
		if(resetting || !simulating)
		{
			return;
		}
		// Don't shoot immediately,
		// Check difference in time (timescale-affected seconds since startup) between function calls.
		// Equivalent to using a countdown timer, but not needing any code in Update().
		if((Time.time - timeOfPrevPlayerShoot) < secsBetweenPlayerShots)
		{
			return; // Previous shot was too recent to shoot again.
		}
		// We can shoot, remember time so we can calculate cooldown for next time
		timeOfPrevPlayerShoot = Time.time;

		// Instantiate a shot, copy spc inv player's position and rotation, parent to this script's transform to keep hierachy tidy.
		PiT_SpaceInvaders_Game_PlayerShot instShot =
			Instantiate(playerShotPrefab, spcInvPlayer.position, spcInvPlayer.rotation, parent: this.transform);

		instShot.SimRef = this;
	}
#endregion


	/// <summary>
	/// Call this function to move the invaders, regardless of timing/speed.
	/// Note that this function does not account for boundary shrinkage via
	///   losing invaders, as that has not been implemented yet.
	/// </summary>
	private void MoveInvaders()
	{
		if(invaderDirLeftElseRight) // If we're moving left
		{
			// If we hit the left edge
			if(IsInvaderInCol(0))
			{
				currInvaderRow--; // move down 1 row
				invaderDirLeftElseRight = false; // start moving right
			}
			else
			{
				currInvaderCol--; // move left
			}
		}
		else // If we're moving right
		{
			// If we hit the right edge
			if(IsInvaderInCol(numMoveCols))
			{
				currInvaderRow--; // move down 1 row
				invaderDirLeftElseRight = true; // start moving left
			}
			else
			{
				currInvaderCol++; // move right
			}
		}

		// Move the invaders
		invadersMovePivot.position = InvaderGridToWorld(currInvaderCol, currInvaderRow);

		// If the invaders have moved past the final row, game over
		// Invert input as the invaders move down
		if(IsInvaderInRow(-(numMoveRows + 1)))
		{
			GameOver(loseText);
		}
	}

	// When an invader dies, it calls this function (and passes itself)
	public void InvaderDead(PiT_SpaceInvaders_Game_Invader inv)
	{
		if(inv == null)
		{
			Debug.LogWarning($"Warning: Passed invader is null!");
			return;
		}
		if(resetting || !simulating)
		{
			return;
		}

		// Remove invader
		instInvaders.Remove(inv);

		// If no more invaders, the player wins
		if(instInvaders.Count == 0)
		{
			GameOver(winText);
			return;
		}

		// Update the movement speed of the invaders
		// -----------------------------------------
		// The move speed of the invaders was dependant on how fast they could
		//   be drawn, so the less invaders there were, the faster they moved.
		// Since we don't have control over the draw calls, we have to fake it.
		// We know the number of invaders remaining, and in total (range A).
		// We specify the speed we want the invaders to move depending on the number of invaders remaining (range B).
		// Range A gets mapped to range B, so by inputting the number of
		//   invaders remaining, we get the speed they should be moving at.
		secsBetweenMove = ClassExtensions.MapRangeToRangeUnclamped(
			rangeAcurrent: instInvaders.Count,
			rangeAFrom:    1,
			rangeATo:      numInvadersTotal,
			rangeBFrom:    invaderSpdOne,
			rangeBTo:      invaderSpdAll
		);
	}
	

	private void GameOver(string textToDisplay)
	{
		simulating = false; // Stop simulating
		displayText.text = textToDisplay;
	}

#if UNITY_EDITOR // Gizmos don't exist outside of the editor, so just remove from binaries entirely
	private void OnDrawGizmosSelected()
	{
		// Draw Player-related Gizmos
		// --------------------------
		if(spcInvPlayer != null && spcInvPlayerCenter != null)
		{
			// Draw the range that the player can move in
			Gizmos.color = Color.red;
			Vector3 playerMaxOffset = spcInvPlayer.TransformVector(Vector3.left * playerMaxHorizontal);
			Gizmos.DrawLine(
				spcInvPlayerCenter.position + playerMaxOffset,
				spcInvPlayerCenter.position - playerMaxOffset
			);
		}

		// Draw Invader-related Gizmos
		// ---------------------------
		if(invaderGrid == null)
		{
			invaderGrid = GetComponent<Grid>(); // guaranteed because required component
		}

		// Draw the total grid area bounds that the invaders can move in
		Gizmos.color = Color.blue;
		Gizmos.DrawLine(invaderGrid.CellToWorld(Vector3Int.zero),      InvaderGridToWorld(numMoveCols, 0           ));
		Gizmos.DrawLine(invaderGrid.CellToWorld(Vector3Int.zero),      InvaderGridToWorld(0,           -numMoveRows));
		Gizmos.DrawLine(InvaderGridToWorld(numMoveCols, -numMoveRows), InvaderGridToWorld(numMoveCols, 0           ));
		Gizmos.DrawLine(InvaderGridToWorld(numMoveCols, -numMoveRows), InvaderGridToWorld(0,           -numMoveRows));

		// Draw the grid of invaders
		Gizmos.color = Color.green;
		Gizmos.DrawLine(invaderGrid.CellToWorld(Vector3Int.zero),                InvaderGridToWorld(numInvaders.x-1, 0                 ));
		Gizmos.DrawLine(invaderGrid.CellToWorld(Vector3Int.zero),                InvaderGridToWorld(0,               -(numInvaders.y-1)));
		Gizmos.DrawLine(InvaderGridToWorld(numInvaders.x-1, -(numInvaders.y-1)), InvaderGridToWorld(numInvaders.x-1, 0                 ));
		Gizmos.DrawLine(InvaderGridToWorld(numInvaders.x-1, -(numInvaders.y-1)), InvaderGridToWorld(0,               -(numInvaders.y-1)));
	}
#endif
}
