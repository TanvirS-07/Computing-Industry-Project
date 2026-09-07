using System.Collections.Generic;
using UnityEngine;

static class ClassExtensions
{
	/// <summary>
	/// https://en.wikipedia.org/wiki/Fisher–Yates_shuffle
	/// </summary>
	public static void Shuffle<T>(this IList<T> list)
	{
		for(int i = 0; i < list.Count - 1; i++)
		{
			int j = UnityEngine.Random.Range(i, list.Count);
			(list[i], list[j]) = (list[j], list[i]); // swap
		}
	}

	public static int LayerMaskToLayer(this LayerMask mask) => (int) Mathf.Log(mask.value, 2);


	/// <summary>
	/// Lerping from <see cref="rangeBFrom"/> to <see cref="rangeBTo"/> where is
	///   the inverse lerp of <see cref="rangeAFrom"/> to <see cref="rangeATo"/> where <see cref="rangeAcurrent"/> is the t value.
	/// </summary>
	/// <param name="rangeAcurrent"></param>
	/// <param name="rangeAFrom"></param>
	/// <param name="rangeATo"></param>
	/// <param name="rangeBFrom"></param>
	/// <param name="rangeBTo"></param>
	/// <returns>When input == <see cref="rangeAFrom"/>, return <see cref="rangeBFrom"/>, when input == <see cref="rangeATo"/>, return <see cref="rangeBTo"/>, and everywhere inbetween.</returns>
	public static float MapRangeToRangeUnclamped(float rangeAcurrent, float rangeAFrom, float rangeATo, float rangeBFrom, float rangeBTo)
	{
		return rangeBFrom + (rangeAcurrent - rangeAFrom) * (rangeBTo - rangeBFrom) / (rangeATo - rangeAFrom);
	}

	/// <summary>
	/// Ensures both the x and y components are at least zero, and then ensures the y component is larger than the x component.
	/// </summary>
	public static Vector2 ValidateAsMinMaxRange(this Vector2 vec)
	{
		// make sure the min and max height are both positive
		if(vec.x < 0.0f) { vec.x = 0.0f; }
		if(vec.y < 0.0f) { vec.y = 0.0f; }
		// make sure max is bigger than min
		if(vec.y < vec.x)
		{
			// swap components
			(vec.x, vec.y) = (vec.y, vec.x);
		}
		return vec;
	}
}