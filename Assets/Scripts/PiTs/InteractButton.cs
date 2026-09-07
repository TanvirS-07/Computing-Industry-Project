using UnityEngine;
using UnityEngine.Events;

public class InteractButton : MonoBehaviour, IPlayerInteractable
{
	[Header(nameof(InteractButton)+" Settings")]
	[SerializeField] private bool eventOnPress = false;
	public UnityEvent<InteractButton> OnPressedEvent = new();

	/*[SerializeField] private bool eventOnHold = false;
	public UnityEvent<InteractButton> OnHeldEvent = new();*/

	[SerializeField] private bool eventOnDepress = false;
	public UnityEvent<InteractButton> OnDepressedEvent = new();

	/*private void Awake()
	{
	    
	}*/

	public void Interact()
	{
		if(!eventOnPress) { return; }

		OnPressedEvent.Invoke(this);
	}

	/*private void Held()
	{
		if(!eventOnHold) { return; }

		OnHeldEvent.Invoke(this);
	}*/

	public void StopInteract()
	{
		if(!eventOnDepress) { return; }

		OnDepressedEvent.Invoke(this);
	}

	// If you want to be able to call a function on a singleton object, you'll have to write a function like this:
	/*public void SingletonFunc()
	{
		if(PlayerControl.InstanceExists())
		{
			PlayerControl.Instance.Func();
		}
	}*/
}