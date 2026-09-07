using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Project Plan (v1.0 onwards) description:
/// Sonic the Hedgehog (1991): Represents the jump in graphics from the Nintendo Entertainment
///   System's 8-bit to the Sega Genesis' 16-bit. During the "Console Wars", Sonic was created to
///   compete against Nintendo's Mario. Sonic then went on to start the trend of video game mascots.
/// </summary>
[DisallowMultipleComponent, RequireComponent(typeof(TextureScroller))]
public class PiT_Sonic : PiT
{
	[Header(nameof(PiT_Sonic)+" Settings")]
	[SerializeField] private Animator speedAnim;
	[Space(10)]
	[SerializeField] private TextureScroller silhouetteScroller;
	[SerializeField] private Material silhouetteMat;
	[Tooltip("The name of the material's property that is used to reveal silhouettes. The slider is expected to go from 0 to 1.")]
	[SerializeField] private string matSilhouetteSliderName;
	[Tooltip("The amount of seconds it takes to reveal the silhouette.")]
	[SerializeField, Min(0.0f)] private float silhouetteRevealSecs = 2.0f;
	[Tooltip("The amount of seconds to linger on the revealed silhouette before hiding and scrolling.")]
	[SerializeField, Min(0.0f)] private float silhouetteLingerSecs = 1.0f;
	[Tooltip("The amount of seconds it takes to hide the silhouette.")]
	[SerializeField, Min(0.0f)] private float silhouetteHideSecs = 0.5f;

	[Header("Exposed values, not settings")]
	//[SerializeField] private float currSecsBetweenScroll;
	[SerializeField] private bool canRevealSilhouette;

	// Awake() is called the moment the object is active in the scene
	protected override void Awake()
	{
		base.Awake();

		// If no texture scroller has been assigned, default to the required one.
		if(silhouetteScroller == null)
		{
			// Guaranteed because required component
			silhouetteScroller = GetComponent<TextureScroller>();
		}

		if(speedAnim == null)
		{  Debug.LogError($"Error: {this} has no {nameof(speedAnim)} assigned!");  }
		if(silhouetteMat == null)
		{  Debug.LogError($"Error: {this} has no {nameof(silhouetteMat)} assigned!");  }
		else if(!silhouetteMat.GetPropertyNames(MaterialPropertyType.Float).Contains(matSilhouetteSliderName))
		{
			Debug.LogError($"Error: {this} couldn't find property in {silhouetteMat} used to reveal silhouettes (trying to find \"{matSilhouetteSliderName}\")!"
				+"All float properties on this shader: {string.Join(',', silhouetteMat.GetPropertyNames(MaterialPropertyType.Float))}.");
		}

		SilhouetteSet(1.0f);
		canRevealSilhouette = true;
	}

	public override void Load()
	{
		base.Load();

		// Load the texture scroller
		silhouetteScroller.Load();

		// Start the speed animation (will automatically transition to entry state)
		speedAnim.enabled = true;
	}

	public override void Unload()
	{
		base.Unload();

		// Unload the silhouette scroller
		silhouetteScroller.Unload();
		StopAllCoroutines();

		// Stop the speed animation
		speedAnim.enabled = false;
	}

	public void RevealAndScroll()
	{
		if(!canRevealSilhouette) { return; }
		canRevealSilhouette = false;
		StartCoroutine(ScrollSilhouettes());
	}

	private IEnumerator ScrollSilhouettes()
	{
		yield return SilhouetteReveal(silhouetteRevealSecs);

		yield return new WaitForSeconds(silhouetteLingerSecs);

		yield return SilhouetteHide(silhouetteHideSecs);
		
		silhouetteScroller.TryScrollNextOverlay();

		while(silhouetteScroller.IsScrolling)
		{
			yield return null; // wait 1 frame
		}

		canRevealSilhouette = true;
	}

	private void SilhouetteSet(float toVal)
		=> silhouetteMat.SetFloat(matSilhouetteSliderName, toVal);

	private IEnumerator SilhouetteReveal(float lengthSeconds)
		=> SilhouetteFade(lengthSeconds, 1.0f, 0.0f);
	private IEnumerator SilhouetteHide(float lengthSeconds)
		=> SilhouetteFade(lengthSeconds, 0.0f, 1.0f);

	private IEnumerator SilhouetteFade(float lengthSeconds, float startVal, float endVal)
	{
		SilhouetteSet(startVal);

		float timeStarted = Time.realtimeSinceStartup;
		float timeElapsed = 0.0f;
		lengthSeconds = Mathf.Max(lengthSeconds, 0.1f);

		do // do-while is just while, but run the code first, then check condition
		{
			SilhouetteSet(
				Mathf.Lerp(startVal, endVal, timeElapsed / lengthSeconds)
			);

			timeElapsed = Mathf.Abs(Time.realtimeSinceStartup - timeStarted);
			yield return null;
		}
		while(timeElapsed < lengthSeconds);

		SilhouetteSet(endVal);
	}
}
