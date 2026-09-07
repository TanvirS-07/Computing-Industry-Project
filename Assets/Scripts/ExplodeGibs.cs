using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ExplodeGibs : MonoBehaviour
{
	/*private static Material dissolveMat;
	public const string DISSOLVE_MATERIAL_FILEPATH = "Misc/Dissolve"; // From inside the Assets/Resources folder

	// https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	private static void GetAllLimbsResources()
	{
		dissolveMat = Resources.Load<Material>(DISSOLVE_MATERIAL_FILEPATH);
		if(dissolveMat == null) { Debug.LogError($"Error: Dissolve {nameof(Material)} for {nameof(ExplodeGibs)} could not be found!"); }
		else { Debug.Log($"Dissolve {nameof(Material)} for {nameof(ExplodeGibs)} loaded!"); }
	}*/

	[SerializeField] private Transform centerOfExplosion;
	[SerializeField] private float explodeForce;
	[SerializeField] private float upwardsForce = 2.0f;
	float explodeRadius;
	[SerializeField, Min(0.0f)] private float secsBeforeHideGibs;
	[SerializeField] private GameObject[] gibs;
	private List<Rigidbody> gibRBs = new();
	private List<Vector3> gibOGPos = new();
	private List<Quaternion> gibOGRot = new();
	//[SerializeField] private MeshRenderer[] missingBits;

	public UnityEvent<ExplodeGibs> StartedExploding = new();
	public UnityEvent<ExplodeGibs> FinishedExploding = new();

	[Header("Exposed values, not settings")]
	[SerializeField] private bool exploding = false;
	[HideInInspector] public bool Exploding { get => exploding; }
	//private bool doneExploding = false;
	//public bool canDissolve = true;
	//public float DissolveFadeInSecs = 0.5f;
	//public float DissolveHoldSecs = 0.1f;
	//public float DissolveFadeOutSecs = 0.5f;



	// Awake() is called the moment the object is active in the scene
	private void Awake()
	{
		if(centerOfExplosion == null)
		{
			Debug.LogWarning($"Warning: {nameof(ExplodeGibs)} \"{this.name}\" has no {nameof(centerOfExplosion)} assigned!");
		}
		if(gibs == null || gibs.Length == 0)
		{
			Debug.LogWarning($"Warning: {nameof(ExplodeGibs)} \"{this.name}\" has no {nameof(gibs)} assigned!");
		}
		/*if(rbToDuplicate == null)
		{
			Debug.LogWarning($"Warning: {nameof(ExplodeGibs)} \"{this.name}\" has no {nameof(rbToDuplicate)} assigned!");
		}
		else
		{
			rbToDuplicate.Sleep();
		}*/

		gibRBs = new();
		gibOGPos = new();
		gibOGRot = new();
		foreach(GameObject gib in gibs)
		{
			InitGib(gib);
		}
	}

	private void InitGib(GameObject gib)
	{
		if(gib == null)
		{
			Debug.LogWarning($"Warning: {this} can't drop missing {nameof(gib)} in {nameof(gibs)} array!");
			return;
		}

		// Obliterate anything on this gib that has anything to do with physics 
		//foreach(Rigidbody rb in gib.GetComponentsInChildren<Rigidbody>(true))
		//{
		//	Destroy(rb);
		//}
		//foreach(Collider col in gib.GetComponentsInChildren<Collider>(true))
		//{
		//	Destroy(col);
		//}
		if(gib.TryGetComponent<Rigidbody>(out Rigidbody gibRB))
		{

		}
		else
		{
			// Put on our own physics
			gibRB = gib.AddComponent<Rigidbody>();
		}
		gibRB.Sleep();
		gibRBs.Add(gibRB);
		gibRB.isKinematic = true;

		gibOGPos.Add(gib.transform.localPosition);
		gibOGRot.Add(gib.transform.localRotation);
	}

	public void DropAllGibs()
	{
		if(!exploding)
		{
			exploding = true;
			//doneExploding = false;
			StartCoroutine(ExplodeAnim());
		}
	}

	public void ResetGibs()
	{
		//if(!doneExploding) { return; }
		//doneExploding = false;

		StopAllCoroutines();
		exploding = false;
		//doneExploding = true;

		for(int i = 0; i < gibRBs.Count; i++)
		{
			Rigidbody gibRB = gibRBs[i];

			gibRB.Sleep();
			gibRB.isKinematic = true;
			gibRB.gameObject.SetActive(true);
			gibRB.transform.SetParent(centerOfExplosion, true);
			gibRB.transform.SetLocalPositionAndRotation(gibOGPos[i], gibOGRot[i]);
		}
	}

	public void SetCenterOfExplosion(Transform newCenter)
	{
		centerOfExplosion = newCenter;
	}

	private IEnumerator ExplodeAnim()
	{
		// Annoyingly, we need to pass in an explosion radius, even though we want it to apply to all of the gibs.
		// Let's just set the radius as the distance to the furthest gib.
		explodeRadius = gibs.Max((gib) => Vector3.Distance(gib.transform.position, centerOfExplosion.position));

		StartedExploding.Invoke(this);

		foreach(Rigidbody gibRB in gibRBs)
		{
			StartCoroutine(DropGib(gibRB));//, dissolve));
		}

		yield return new WaitForSeconds(secsBeforeHideGibs);

		//doneExploding = true;
		exploding = false;

		FinishedExploding.Invoke(this);
	}

	// Specifically because inspector events cannot see functions with more than one parameter
	private IEnumerator DropGib(Rigidbody gibRB)
		=> DropGib(gibRB, UnityEngine.Random.rotation.eulerAngles);

	private IEnumerator DropGib(Rigidbody gibRB, Vector3 torqueForce)
	{
		gibRB.transform.SetParent(null, true);
		gibRB.isKinematic = false;
		gibRB.WakeUp();
		gibRB.AddExplosionForce(explodeForce, centerOfExplosion.position, explodeRadius, upwardsForce, ForceMode.VelocityChange);
		gibRB.AddForce(Vector3.up * upwardsForce, ForceMode.Impulse); // add to the up movement from the explosion
		gibRB.AddTorque(torqueForce, ForceMode.Impulse);

		//if(!canDissolve)
		//{
			yield return new WaitForSeconds(secsBeforeHideGibs);
			gibRB.gameObject.SetActive(false);
			gibRB.Sleep();
			gibRB.isKinematic = true;
			gibRB.transform.SetParent(centerOfExplosion, true);
		//}
		//else
		//{
		//	StartCoroutine(ActivateDissolve(gibRB.gameObject));
		//}
	}

	/*private IEnumerator ActivateDissolve(GameObject gib)
	{
		MeshRenderer[] renderers = gib.GetComponentsInChildren<MeshRenderer>(true);
		if(renderers == null || renderers.Length == 0)
		{
			// ok... it's weird to drop a gib that doesn't have a renderer, but ok.

			// hide gib after the same amount of time it would have taken to run this coroutine
			yield return new WaitForSeconds(DissolveFadeInSecs + DissolveHoldSecs + DissolveFadeOutSecs);
			gib.SetActive(false);

			yield break; // finish coroutine
		}

		float dissolveValue;
		float timeElapsed = 0.0f;

		// The dissolve shader is set on top of the enemy's mesh renderer material
		// make it the first index so we can access it via .material (easier than .materials)
		foreach(MeshRenderer r in renderers)
		{
			r.materials = r.materials.Prepend(dissolveMat).ToArray();
		}

		// fades in from nothing to a blue color
		while(timeElapsed < DissolveFadeInSecs)
		{
			dissolveValue = (timeElapsed / DissolveFadeInSecs);
			foreach(MeshRenderer r in renderers)
			{ r.material.SetFloat("_DissolveStrength", dissolveValue); }
			yield return null;
			timeElapsed += Time.deltaTime;
		}

		// When the enemy is completely blue, its original mesh texture is deleted
		List<Material> l = new() { dissolveMat };
		foreach(MeshRenderer r in renderers)
		{
			r.SetMaterials(l);
			r.material.SetFloat("_DissolveStrength", 1.0f);
		}

		// Hold
		timeElapsed = 0.0f;
		while(timeElapsed < DissolveHoldSecs)
		{
			yield return null;
			timeElapsed += Time.deltaTime;
		}

		// The dissolve shader then works in reverse
		timeElapsed = 0.0f;
		while(timeElapsed < DissolveFadeOutSecs)
		{
			dissolveValue = 1.0f - (timeElapsed / DissolveFadeOutSecs);
			foreach(MeshRenderer r in renderers)
			{ r.material.SetFloat("_DissolveStrength", dissolveValue); }
			yield return null;
			timeElapsed += Time.deltaTime;
		}
		
		gib.SetActive(false);
	}*/
}
