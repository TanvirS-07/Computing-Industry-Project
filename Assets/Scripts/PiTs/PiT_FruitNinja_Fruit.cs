using System.Collections;
using UnityEngine;

/// <summary>
/// <para>See also: <seealso cref="PiT_FruitNinja"/></para>
/// </summary>
public class PiT_FruitNinja_Fruit : MonoBehaviour
{
	[Header(nameof(PiT_FruitNinja_Fruit)+" Settings")]
	[SerializeField] private Transform visualsRotParent;
	[Space(5)]
	[SerializeField] private Rigidbody halfOne;
	[SerializeField] private Rigidbody halfTwo;
	[Space(5)]
	[SerializeField, Min(0.0f)] private float sliceForce = 1.0f;

	[Header("Exposed values, not settings")]
	[SerializeField] private PiT_FruitNinja linkedPiT;
	[HideInInspector] public PiT_FruitNinja LinkedPiT { get => linkedPiT; }
	[SerializeField] private Vector3 startPos;
	[HideInInspector] public Vector3 StartPos { get => startPos; }
	[SerializeField] private Vector3 endPos;
	[SerializeField] private float peakHeight; // How high will the peak of the flight be
	[SerializeField] private float flightDurationSecs; // How long the current flight wil ltake
	[SerializeField] private Vector3 axisOfRot;
	[SerializeField] private float rotSpeed;
	[SerializeField] private bool inFlight = false;
	private Vector3 halfOne_StartLocalPos;
	private Quaternion halfOne_StartLocalRot;
	private Vector3 halfTwo_StartLocalPos;
	private Quaternion halfTwo_StartLocalRot;
	[SerializeField] private bool gotSliced = false;

	/// <summary>
	/// Stand-in for Start()/Awake(), but controlled by the <see cref="PiT_FruitNinja"/> passed into this function.
	/// </summary>
	/// <param name="linkWithPiT">The <see cref="PiT_FruitNinja"/> to reference and be controlled by.</param>
	public void Init(PiT_FruitNinja linkWithPiT)
	{
		linkedPiT = linkWithPiT; // link with Fruit Ninja PiT

		// Effectively disable rigidbodies
		halfOne.Sleep();
		halfOne.isKinematic = true;
		halfTwo.Sleep();
		halfTwo.isKinematic = true;

		if(halfOne == null || halfTwo == null)
		{
			Debug.LogError($"Error: {this} is missing one or both halves ({nameof(halfOne)} or {nameof(halfTwo)}) in inspector!");
		}
		else
		{
			// remember the initial positions and rotations of the halves so we can restore them the next time the fruit flies
			halfOne_StartLocalPos = halfOne.transform.localPosition;
			halfOne_StartLocalRot = halfOne.transform.localRotation;
			halfTwo_StartLocalPos = halfTwo.transform.localPosition;
			halfTwo_StartLocalRot = halfTwo.transform.localRotation;
		}
	}

	/// <summary>
	/// If the fruits' gravity seems off, you will need to calculate <see cref="flightDurationSecs"/> based
	///   on the parabola function (<see cref="ParabolaFunction"/>), which will require taking the second derivative.
	/// </summary>
	/// <param name="startPos"></param>
	/// <param name="endPos"></param>
	/// <param name="peakHeight"></param>
	/// <param name="flightDurationSecs"></param>
	/// <param name="axisOfRot"></param>
	/// <param name="rotSpeed"></param>
	public void Launch(Vector3 startPos, Vector3 endPos, float peakHeight, float flightDurationSecs, Vector3 axisOfRot, float rotSpeed)
	{
		if(inFlight) { return; }

		// Set per-flight values
		// ---------------------
		this.startPos = startPos;
		this.endPos = endPos;
		endPos.y = startPos.y; // Make sure the start and end points are the same altitude
		para_D = Vector3.Distance(startPos, endPos);

		this.peakHeight = peakHeight;
		this.flightDurationSecs = flightDurationSecs;

		this.axisOfRot = axisOfRot;
		this.rotSpeed = rotSpeed;

		// Start flying
		// ------------
		inFlight = true;
		StartCoroutine(Fly());
	}

	private float para_D; // The distance between the start and end points (see Parabolic function at the bottom)
	private float currTime;
	private float currHeight = 0.0f;
	[HideInInspector] public float CurrentHeight { get => currHeight; }
	private float lerp_t; // lerp t value of time elapsed

	private IEnumerator Fly()
	{
		//Debug.Log("startPos: " + startPos + ",  endPos: " + endPos + ",  peakHeight: " + peakHeight + ",  para_D: " + para_D + ",  dur: " + flightDurationSecs);
		currTime = 0.0f;
		while(currTime < flightDurationSecs)
		{
			currTime += Time.deltaTime;
			// Get the current progress of the flight. This is linear.
			lerp_t = currTime / flightDurationSecs;

			// A parabola's x and y coordinates are independant of each other,
			//   and the x coordinate moves at a constant rate, so we can use
			//   the x coordinate to calculate the y coordinate.
			currHeight = ParabolaFunction(para_D * lerp_t); // input current horizontal distance travelled

			// Position the fruit
			transform.position = Vector3.Lerp(startPos, endPos, lerp_t);
			transform.Translate(0.0f, currHeight, 0.0f); // Place at correct height

			// Rotate the fruit visuals
			visualsRotParent.Rotate(axisOfRot, rotSpeed * Time.deltaTime);

			//Debug.Log(transform.position + ",  currHeight: " + currHeight + ",  lerp_t: " + lerp_t);

			yield return null; // Wait 1 frame
		}

		// Restore the fruit if it got sliced
		if(gotSliced)
		{
			// Effectively disable the rigidbodies and restore initial pos/rot.
			halfOne.Sleep();
			halfOne.isKinematic = true;
			halfOne.transform.SetLocalPositionAndRotation(halfOne_StartLocalPos, halfOne_StartLocalRot);
			halfTwo.Sleep();
			halfTwo.isKinematic = true;
			halfTwo.transform.SetLocalPositionAndRotation(halfTwo_StartLocalPos, halfTwo_StartLocalRot);

			gotSliced = false;
		}

		// Flag that we're done
		inFlight = false;
		linkedPiT.LaunchFruitEnd(this);
	}


	/*\
	|*| This function implements this formula:
	|*| 
	|*|   y = -4 * (y_max / D^2) * x * (x - D)
	|*| 
	|*| Where:
	|*|   y_max = the height of the peak of the parabola (the biggest the y coordinate will get)
	|*|   D     = The distance between the start and end points
	|*| 
	|*| On a 2D cartesian coordinate plane, any 3 noncollinear points defines a unique parabola.
	|*| In this formula, these three points would be:
	|*|   start point      S = (0, 0)
	|*|   end point        E = (D, 0)
	|*|   peak of parabola H = (D/2, y_max), or alternatively ((S.x+E.x)/2, y_max)
	|*| 
	|*| Because 
	\*/
	private float ParabolaFunction(float currDist)
		=> -4.0f * (peakHeight / (para_D * para_D)) * currDist * (currDist - para_D);


	/// <summary>
	/// Call this to slice the fruit.
	/// </summary>
	/// <returns>Whether or not it was able to be sliced.</returns>
	public bool Slice()
	{
		if(!inFlight || gotSliced)
		{
			// If we're not flying or the fruit was already sliced
			return false;
		}

		// Effectively enable the rigidbodies
		halfOne.WakeUp();
		halfOne.isKinematic = false;
		halfTwo.WakeUp();
		halfTwo.isKinematic = false;

		// Calculate the force to apply to the halves, and make them fly away from each other.
		// Since the vector is (to - from), this is the force for
		//   half one. Negate this force to get the force for half two.
		Vector3 halfOneForce = (halfOne.position - halfTwo.position).normalized * sliceForce;
		halfOne.AddForce(halfOneForce, ForceMode.Force);
		halfTwo.AddForce(-halfOneForce, ForceMode.Force);

		Debug.Log($"{this} sliced.");
		gotSliced = true;
		return true; // Successfully sliced
	}
}
