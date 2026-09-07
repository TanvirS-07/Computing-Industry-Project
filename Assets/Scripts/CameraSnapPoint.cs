using UnityEngine;

[DisallowMultipleComponent]
public class CameraSnapPoint : MonoBehaviour
{
	[SerializeField] private string displayName = nameof(CameraSnapPoint); // default the string to the class name
	[HideInInspector] public string DisplayName { get => displayName; }

	[SerializeField] private Color gizmoCol = Color.white;

	private void OnDrawGizmos()
	{
		Gizmos.color = gizmoCol;
		Gizmos.DrawWireSphere(transform.position, 0.1f);
		Gizmos.DrawLine(transform.position, transform.position + transform.forward);
	}

	private void OnDrawGizmosSelected()
	{
		if(!PlayerControl.InstanceExists()) { return; }

		Camera cam = PlayerControl.Instance.PlayerCamera;
		if(cam == null) { return; }
		
		Gizmos.color = gizmoCol;
		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane, cam.nearClipPlane, cam.aspect);
	}
}
