using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class Plaque : DistanceLoader, IPlayerInteractable
{
	[Header("Plaque Content")]
	[Tooltip("The GameObject that contains the plaque text / UI / content to show and hide.")]
	[SerializeField] private Billboard billboard;
	[SerializeField] private TMP_Text plaqueContent;
	[SerializeField] private bool rotateFacePlayer = false;
	[Space(5)]
	[SerializeField] protected bool autoHideTimer = false;
	[SerializeField] protected float autoHideAfterSecs = 5.0f;

	[Header("Exposed values, not settings")]
	// Tracks whether the plaque content is currently visible.
	[SerializeField] private bool isVisible = false;
	[SerializeField] protected float currAutoHideAfterSecs;
	
	// Runs when the object is first created in the scene and ensures the plaque starts hidden.
	private void Awake() 
	{
		if(billboard == null) 
		{  Debug.LogError($"Error: {this} has no {nameof(billboard)} assigned! (root: \"{transform.root.name}\")");  }
		if(plaqueContent == null) 
		{  Debug.LogError($"Error: {this} has no {nameof(plaqueContent)} assigned! (root: \"{transform.root.name}\")");  }

		ResetPlaque();
	}

	// OnDisable() is called when the object is disabled
	// Runs when the object is disabled and resets the plaque so it does not stay open unexpectedly.
	private void OnDisable() 
	{
		ResetPlaque();
	}

	public override void Unload()
	{
		base.Unload();
		// Automatically hide the plaque when out of range
		HidePlaque();
	}

	// Update is called once per frame
	protected override void Update()
	{
		base.Update();

		CheckAutoHideTimer();
	}

	protected virtual void CheckAutoHideTimer()
	{
		if(!autoHideTimer || !isVisible) { return; }

		currAutoHideAfterSecs -= Time.deltaTime;

		if(currAutoHideAfterSecs > 0.0f) { return; }

		HidePlaque();
	}

	/// <summary>
	/// Shows the plaque content.
	/// </summary>
	public void ShowPlaque() 
	{
		isVisible = true;

		billboard.enabled = isVisible && rotateFacePlayer;
		billboard.BillboardRotPivot.gameObject.SetActive(isVisible);

		if(autoHideTimer)
		{
			currAutoHideAfterSecs = autoHideAfterSecs;
		}
	}

	/// <summary>
	/// Hides the plaque content.
	/// </summary>
	public void HidePlaque() 
	{
		isVisible = false;

		billboard.BillboardRotPivot.gameObject.SetActive(isVisible);
		billboard.enabled = isVisible && rotateFacePlayer;
	}

	/// <summary>
	/// Toggles the plaque content between visible and hidden.
	/// Called by PlayerControl when the player interacts with this object.
	/// </summary>
	public void Interact()
	{
		if(isVisible)
		{
			HidePlaque();
		}
		else
		{
			ShowPlaque();
		}
	}
	public void StopInteract()
	{
		
	}

	/// <summary>
	/// Resets the plaque to its default hidden state.
	/// </summary>
	public void ResetPlaque() 
	{
		HidePlaque();
	}

	/// <summary>
	/// Returns whether the plaque content is currently visible.
	/// </summary>
	public bool IsVisible() 
	{
		return isVisible;
	}
}
