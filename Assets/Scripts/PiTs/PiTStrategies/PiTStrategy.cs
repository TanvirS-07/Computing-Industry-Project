using System.Collections;
using UnityEngine;

public struct SnapPoint
{
	public Vector3 snapPosWorld;
	public Quaternion snapRotWorld;
}

/// <summary>
/// <para>The Strategy Pattern that provides the list of placements of each PiT's RoomSnapPoint in world space.</para>
/// </summary>
[DisallowMultipleComponent]
public abstract class PiTStrategy : MonoBehaviour, IEnumerable, IEnumerator
{
	object IEnumerator.Current { get => GetCurrent(); }
	public SnapPoint Current { get => GetCurrent(); }

	// These three functions are implemented per-strategy
	// --------------------------------------------------
	/// <returns>The current Snap Point.</returns>
	protected abstract SnapPoint GetCurrent();

	/// <summary>Try to move to the next snap point</summary>
	/// <returns>whether it has successfuly moved to the next snap point</returns>
	public abstract bool MoveNext();

	/// <summary>
	/// Reset the list generation.
	/// </summary>
	public abstract void Reset();


	IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) GetEnumerator();
	public IEnumerator GetEnumerator()
	{
		Reset();
		return this;
	}


#if UNITY_EDITOR
	// Per-strategy gizmo drawing, called by whatever is using this strategy.
	public virtual void DrawGizmos(Transform trans) {}
#endif
}

