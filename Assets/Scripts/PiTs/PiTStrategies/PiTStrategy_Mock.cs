using UnityEngine;

/// <summary>
/// <inheritdoc/>
/// This strategy is a mock. It always returns <see cref="Vector3.zero"/> for positions and <see cref="Quaternion.identity"/> for rotations.
/// </summary>
public class PiTStrategy_Mock : PiTStrategy
{
	public override bool MoveNext()
	{
		return true; // Always possible to get something
	}

	public override void Reset()
	{
		// Nothing to reset
	}

	protected override SnapPoint GetCurrent()
	{
		return new SnapPoint()
		{
			snapPosWorld = Vector3.zero,
			snapRotWorld = Quaternion.identity,
		};
	}
}
