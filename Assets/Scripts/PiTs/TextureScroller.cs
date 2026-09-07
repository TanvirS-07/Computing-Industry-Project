using System.Collections;
using UnityEngine;

public class TextureScroller : MonoBehaviour
{
	[SerializeField, Min(1)] private int numOverlays;
	[Space(5)]
	[SerializeField] private MeshRenderer overlayRend;
	[SerializeField] private Material overlayMat;
	[SerializeField] private bool scrollVertical;
	[Tooltip("Auto-calculate the scroll offset of the overlay material")]
	[SerializeField] private bool forceScrollOffset;
	//[SerializeField] private bool scrollPosDir;
	[Tooltip("How many seconds it takes to scroll to the next overlay")]
	[SerializeField, Min(0.0f)] private float scrollTimeSeconds;
	[Tooltip("Smoothing of scrolling to the next overlay. Note that the code will force the start and end points to (0,0) and (1,1).")]
	[SerializeField] private AnimationCurve scrollSmooth;
	private float currScrollTime;
	private float startScroll;
	private float endScroll;
	private float lerp_t;
	private Vector2 tmp;
	[Header("Exposed values, not settings")]
	[SerializeField] private bool isScrolling;
	[HideInInspector] public bool IsScrolling { get => isScrolling; }
	[SerializeField] private int currOverlay = 0;

#region OnValidate Function
	protected void OnValidate()
	{
		if(overlayMat == null || overlayMat.mainTexture == null) { return; } // all following code needs these

		// Fit the overlays texture to the mesh
		if(forceScrollOffset)
		{
			UpdateOverlayScale();
		}

		// Validate the scroll smoothing animation curve
		// =============================================
		if(scrollSmooth == null) { return; } // all following code validates the animation curve

		while(scrollSmooth.keys.Length < 2) // force at least 2 keys
		{
			// Place them all in a line so they aren't overlapping in either direction
			scrollSmooth.AddKey(scrollSmooth.keys.Length, scrollSmooth.keys.Length);
		}

		// Shift all keys such that the first key has a time and value of 0.
		// -----------------------------------------------------------------
		if(scrollSmooth.keys[0].time != 0.0f) // Only perform this if the first keyframe's time is not already 0.
		{
			System.Span<Keyframe> kfs = new(scrollSmooth.keys);

			// Translate all keyframes by this offset, which makes the first keyframe land on 0.
			float offset = kfs[0].time;
			for(int i = 0; i < kfs.Length; i++) {  kfs[i].time -= offset;  }

			scrollSmooth.SetKeys(kfs);
		}
		if(scrollSmooth.keys[0].value != 0.0f) // Only perform this if the first keyframe's value is not already 0.
		{
			System.Span<Keyframe> kfs = new(scrollSmooth.keys);

			// Translate all keyframes by this offset, which makes the first keyframe land on 0.
			float offset = kfs[0].value;
			for(int i = 0; i < kfs.Length; i++) {  kfs[i].value -= offset;  }

			scrollSmooth.SetKeys(kfs);
		}

		// Then, scale all keys such that the last key has a time and value of 1.
		// ----------------------------------------------------------------------
		if(scrollSmooth.keys[^1].time != 1.0f) // Only perform this if the last keyframe's time is not already 1.
		{
			System.Span<Keyframe> kfs = new(scrollSmooth.keys);

			// Get last keyframe's value, and make sure we don't divide by 0.
			float scalar = Mathf.Max(kfs[^1].time, 0.001f);
			// Scale all keyframes by this scalar so they fit between 0 and 1 inclusive.
			for(int i = 0; i < kfs.Length; i++) {  kfs[i].time /= scalar;  }

			scrollSmooth.SetKeys(kfs);
		}
		if(scrollSmooth.keys[^1].value != 1.0f) // Only perform this if the last keyframe's value is not already 1.
		{
			System.Span<Keyframe> kfs = new(scrollSmooth.keys);

			// Get last keyframe's value, and make sure we don't divide by 0.
			float scalar = Mathf.Max(kfs[^1].value, 0.001f);
			// Scale all keyframes by this scalar so they fit between 0 and 1 inclusive.
			for(int i = 0; i < kfs.Length; i++) {  kfs[i].value /= scalar;  }

			scrollSmooth.SetKeys(kfs);
		}
	}
	// Basically should only called by OnValidate() since the values are decided by other inspector values.
	private void UpdateOverlayScale()
	{
		if(scrollVertical)
		{
			tmp.x = 1.0f;
			tmp.y = 1.0f / numOverlays;
		}
		else
		{
			tmp.x = 1.0f / numOverlays;
			tmp.y = 1.0f;
		}
		overlayMat.mainTextureScale = tmp;
	}
#endregion

	// Awake() is called the moment the object is active in the scene
	private void Awake()
	{
		if(overlayRend == null)
		{  Debug.LogError($"Error: \"{gameObject.name}\" ({nameof(TextureScroller)}) has no {nameof(overlayRend)} assigned!");  }
		if(overlayMat == null)
		{  Debug.LogError($"Error: \"{gameObject.name}\" ({nameof(TextureScroller)}) has no {nameof(overlayMat)} assigned!");  }

		// In case we overshoot
		scrollSmooth.preWrapMode = WrapMode.Clamp;
		scrollSmooth.postWrapMode = WrapMode.Clamp;

		// Since the code modifies the material directly, it persists after exiting play mode.
		//   Save the asset's scale and offset, to restore when exiting play mode.
		// The texture offset on startup is also assumed to be the default offset, and so is treated as "zero" offset.
		ogMainTexOff = overlayMat.mainTextureOffset;
		// Don't actually need to do the scale (see OnValidate()), but may as well
		// This is mainly because Git picks up every time this file changes.
		ogMainTexScale = overlayMat.mainTextureScale;

		SetScrollOffset(0.0f);
	}

	private Vector2 ogMainTexOff;
	private Vector2 ogMainTexScale;
	protected void OnDestroy()
	{
		// Restore asset's scale and offset.
		overlayMat.mainTextureScale = ogMainTexScale;
		overlayMat.mainTextureOffset = ogMainTexOff;
	}

	public void Load()
	{
		// Enable the transparent mesh
		overlayRend.enabled = true;
	}

	public void Unload()
	{
		// If currently scrolling, stop
		StopAllCoroutines();
		isScrolling = false;
		
		// Disable the transparent mesh so we don't have to render it
		overlayRend.enabled = false;
	}

	public void TryScrollNextOverlay()
	{
		// Already Scrolling
		if(isScrolling) { return; }

		isScrolling = true;
		StartCoroutine(ScrollNextOverlay());
	}

	private IEnumerator ScrollNextOverlay()
	{
		// these are the start and end texture offsets of the scroll.
		startScroll = GetScrollOffset();
		endScroll = GetScrollOffset() + (1.0f / numOverlays);

		currScrollTime = 0.0f;
		while(currScrollTime < scrollTimeSeconds)
		{
			currScrollTime += Time.deltaTime;

			// Get the current progress of the scroll. This is linear (no smoothing)
			lerp_t = currScrollTime / scrollTimeSeconds;

			// Convert the lerp time input from linear to the specified smooth curve.
			// The anim curve is forced to be from 0 to 1 in OnValidate(), so we can
			//   use it as the lerp time input (reuse the same varaible for convenience).
			lerp_t = scrollSmooth.Evaluate(lerp_t);
			
			// Evaluate and set the current texture scroll offset.
			// Used unclamped lerp in case someone wants the scroll to overshoot/follow-through.
			SetScrollOffset(
				Mathf.LerpUnclamped(startScroll, endScroll, lerp_t));

			yield return null; // Wait 1 frame
		}

		SetScrollOffset(endScroll); // Snap to the next overlay

		currOverlay++; // We're now on the next overlay
		if(currOverlay >= numOverlays)
		{
			currOverlay -= numOverlays; // Wrap the index around
			SetScrollOffset(GetScrollOffset() - 1); // Wrap around
		}

		// Flag that we're done
		isScrolling = false;
	}

	private void SetScrollOffset(float off)
	{
		tmp = ogMainTexOff;
		if(scrollVertical)
		{
			tmp.y += off;
		}
		else
		{
			tmp.x += off;
		}
		overlayMat.mainTextureOffset = tmp;
	}
	private float GetScrollOffset()
	{
		return (scrollVertical) ? overlayMat.mainTextureOffset.y : overlayMat.mainTextureOffset.x;
	}
}
