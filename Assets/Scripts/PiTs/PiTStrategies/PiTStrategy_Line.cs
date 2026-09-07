using UnityEngine;

/// <summary>
/// <inheritdoc/>
/// This strategy makes every <see cref="PiT"/> form a line in world space.
/// The first <see cref="PiT.RoomSnapPoint"/> will have the same position and rotation as the <see cref="firstSnapPoint"/> field.
/// Subsequent <see cref="PiT.RoomSnapPoint"/>s will be offset by the <see cref="worldOffset"/> field.
/// </summary>
[System.Serializable, DisallowMultipleComponent]
public class PiTStrategy_Line : PiTStrategy
{
	[Header("This strategy makes every " + nameof(PiT) + " form a line in world\nspace. The first "
		+ nameof(PiT.RoomSnapPoint) + " will have the same\nposition and rotation as the " + nameof(firstSnapPoint)
		+ " field.\nSubsequent " + nameof(PiT.RoomSnapPoint) + "s will be offset by the\n" + nameof(worldOffset) + " field.")]
	[SerializeField] private Transform firstSnapPoint;
	[SerializeField] private Vector3 worldOffset;
	
	private Vector3 currPos; // The current world position of the strategy.

	public override bool MoveNext()
	{
		currPos += worldOffset;
		return true; // Always possible to get the next one
	}

	public override void Reset()
	{
		if(firstSnapPoint == null)
		{
			Debug.LogError($"Error: {nameof(PiTStrategy_Line)}'s {nameof(firstSnapPoint)} has not been assigned!");
			return;
		}

		currPos = firstSnapPoint.transform.position;
		// Enumerators start before the first element, because MoveNext() is called before the first "GetCurrent()".
		currPos -= worldOffset;
	}

	protected override SnapPoint GetCurrent()
	{
		return new SnapPoint()
		{
			snapPosWorld = currPos,
			snapRotWorld = firstSnapPoint.rotation,
		};
	}


#if UNITY_EDITOR
	// Per-strategy gizmo drawing, called by whatever is using this strategy.
	public override void DrawGizmos(Transform trans)
	{
		base.DrawGizmos(trans);

		if(firstSnapPoint == null) { return; }

		// Draw a red sphere around the start point, and draw a red line to it.
		Gizmos.color = Color.red;
		Gizmos.DrawLine(trans.position, firstSnapPoint.position);
		Gizmos.DrawSphere(firstSnapPoint.position, 0.5f);

		// Draw a yellow sphere around the second point, and draw a yellow line to it from the start.
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(firstSnapPoint.position, firstSnapPoint.position + worldOffset);
		Gizmos.DrawSphere(firstSnapPoint.position + worldOffset, 0.5f);
	}
#endif
}
