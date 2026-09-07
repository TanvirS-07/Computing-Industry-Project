using UnityEngine;

/// <summary>
/// Project Plan (v1.0 onwards) description:
/// Magnavox Odyssey System (1972): The first video game console.
///    Graphics consisted almost entirely of a transparent overplay placed over the display. 
/// </summary>
[DisallowMultipleComponent, RequireComponent(typeof(TextureScroller))]
public class PiT_Magnavox : PiT
{
	[Header(nameof(PiT_Magnavox)+" Settings")]
	[SerializeField] private TextureScroller texScroller;

	// Awake() is called the moment the object is active in the scene
	protected override void Awake()
	{
		base.Awake();

		// If no texture scroller has been assigned, default to the required one.
		if(texScroller == null)
		{
			// Guaranteed because required component
			texScroller = GetComponent<TextureScroller>();
		}
	}

	public override void Load()
	{
		base.Load();

		// Load the texture scroller
		texScroller.Load();
	}

	public override void Unload()
	{
		base.Unload();

		// Unload the texture scroller
		texScroller.Unload();
	}
}
