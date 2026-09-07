using System.Collections;
using UnityEngine;

/// <summary>
/// Project Plan (v1.0 onwards) description:
/// Fortnite (2017-): Representative of AAA titles, Live Service Games, 
///   the battle royale genre, and normalisation of microtransactions.
/// </summary>
[DisallowMultipleComponent]
public class PiT_Fortnite : PiT
{
	[Header(nameof(PiT_Fortnite)+" Settings")]
	[SerializeField] private ExplodeGibs llamaAnim;
	//[Tooltip("The string name of the trigger in the "+nameof(llamaAnim)+" ("+nameof(ExplodeGibs)+") to open the llama.")]
	//[SerializeField] private string llamaTriggerName_Open;
	[Space(5)]
	[SerializeField] private SpriteRenderer llamaLootRenderer;
	[Space(10)]
	[SerializeField] private Sprite[] allLlamaLoot = new Sprite[0];

	[Header("Exposed values, not settings")]
	[SerializeField] private int lootIndex;

	// Awake() is called the moment the object is active in the scene
	protected override void Awake()
	{
		base.Awake();

		if(llamaAnim == null)
		{  Debug.LogError($"Error: {this} has no {nameof(llamaAnim)} assigned!");  }
		if(llamaLootRenderer == null)
		{  Debug.LogError($"Error: {this} has no {nameof(llamaLootRenderer)} assigned!");  }

		if(allLlamaLoot == null || allLlamaLoot.Length < 1)
		{  Debug.LogError($"Error: {this} needs at least one loot in {nameof(allLlamaLoot)} array!");  }

		llamaAnim.FinishedExploding.AddListener((ctx) => LlamaFinishedExploding());
	}

	public override void Load()
	{
		base.Load();

		// Enable llama
		llamaLootRenderer.gameObject.SetActive(true);
		//llamaAnim.enabled = true; // Animator transitions into entry state
		llamaAnim.gameObject.SetActive(true);
		llamaLootRenderer.sprite = null;
		llamaAnim.ResetGibs();
	}

	public override void Unload()
	{
		base.Unload();

		// Disable the llama and animator and loot
		//llamaAnim.enabled = false;
		llamaAnim.gameObject.SetActive(false);
		llamaLootRenderer.gameObject.SetActive(false);
	}

	public void OpenLlama()
	{
		// only open the llama if the PiT is loaded and the llama is ready
		if(!isLoaded || llamaAnim.Exploding) { return; }

		// fill the llama with random loot
		lootIndex = UnityEngine.Random.Range(0, allLlamaLoot.Length);
		llamaLootRenderer.sprite = allLlamaLoot[lootIndex];

		// trigger the opening animation of the llama
		//llamaAnim.SetTrigger(llamaTriggerName_Open);
		llamaAnim.DropAllGibs();
	}

	private void LlamaFinishedExploding()
	{
		llamaAnim.ResetGibs();
		llamaLootRenderer.sprite = null;
	}
}
