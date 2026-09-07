using UnityEngine;

/// <summary>
/// <inheritdoc/>
/// This strategy has a list of pre-determined world positions and world rotations for each
///   <see cref="PiT"/>. It does not provide snap points beyond the ones in the <see cref="snapPoints"/> list.
/// The first <see cref="PiT.RoomSnapPoint"/> will have the same position
///   and rotation as the first index of <see cref="snapPoints"/>, and so on.
/// </summary>
[System.Serializable, DisallowMultipleComponent]
public class PiTStrategy_Predetermined : PiTStrategy
{
	[Header("This strategy has a list of pre-determined world positions and world rotations\nfor each "+nameof(PiT)
		+". It does not provide snap points beyond the ones in the "+nameof(snapPoints)+" list. The first "
		+nameof(PiT.RoomSnapPoint)+" will have the same position\nand rotation as the first index of "+nameof(snapPoints)+", and so on.")]
	[SerializeField] private Transform[] snapPoints;
	
	private int currInd; // The current index of the strategy.

	public override bool MoveNext()
	{
		currInd++;
		return (currInd < snapPoints.Length); // Only possible to get the next one if in bounds.
	}

	public override void Reset()
	{
		if(snapPoints == null || snapPoints.Length == 0)
		{
			Debug.LogError($"Error: {nameof(PiTStrategy_Predetermined)} has no {nameof(snapPoints)}!");
			return;
		}

		// Enumerators start before the first element, because MoveNext() is called before the first "GetCurrent()".
		currInd = -1;
	}

	protected override SnapPoint GetCurrent()
	{
		return new SnapPoint()
		{
			snapPosWorld = snapPoints[currInd].position,
			snapRotWorld = snapPoints[currInd].rotation,
		};
	}


#if UNITY_EDITOR
	// Per-strategy gizmo drawing, called by whatever is using this strategy.
	public override void DrawGizmos(Transform trans)
	{
		base.DrawGizmos(trans);

		if(snapPoints == null || snapPoints.Length == 0) { return; }

		// Draw lines between all snap points
		// ----------------------------------

		Transform prev = trans; // First line is from the trans to first snap point...
		Gizmos.color = Color.red; // ...it will be red.

		foreach(Transform sP in snapPoints)
		{
			// Draw line from the previous snap point
			Gizmos.DrawLine(prev.position, sP.position);
			
			// Make the rest of the lines yellow
			Gizmos.color = Color.yellow;

			// Draw a yellow sphere at each snap point
			Gizmos.DrawSphere(sP.position, 0.25f);

			prev = sP;
		}
	}
#endif
}
