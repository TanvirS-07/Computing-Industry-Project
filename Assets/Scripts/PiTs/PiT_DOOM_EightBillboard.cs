using UnityEngine;

/*\
|*|  Information from https://zdoom.org/w/index.php?title=Sprite#Angles
|*|  Ignore the green outer ring in the link, that is ZDoom exclusive, and not a feature of DOOM I or II.
|*|  
|*|   [4][5][6]
|*|   [3]   [7]
|*|   [2][1][8]
|*|  
|*|  1: Camera looking at front of entity
|*|  3: Camera looking at left side of entity
|*|  5: Camera looking at back of entity
|*|  6: Camera looking at right side of entity
|*|  etc.
\*/
[DisallowMultipleComponent]
public class PiT_DOOM_EightBillboard : Billboard
{
	[Header(nameof(PiT_DOOM_EightBillboard)+" Settings")]
	[SerializeField] protected MeshRenderer eightSidedBillboard;
	[SerializeField] private Transform entitydirectionArrow;
	[SerializeField] private float rotSpeed;
	[SerializeField] private Transform towardsPlayerArrow;
	[Space(5)]
	[Tooltip("This is the image shown when the camera is looking at the front of the entity.")]
	[SerializeField] private Texture img_1_front;
	[Tooltip("This is the image shown when the camera is looking at the front-left side of the entity.")]
	[SerializeField] private Texture img_2_frontleft;
	[Tooltip("This is the image shown when the camera is looking at the left side of the entity.")]
	[SerializeField] private Texture img_3_left;
	[Tooltip("This is the image shown when the camera is looking at the back-left side of the entity.")]
	[SerializeField] private Texture img_4_backleft;
	[Tooltip("This is the image shown when the camera is looking at the back of the entity.")]
	[SerializeField] private Texture img_5_back;
	[Tooltip("This is the image shown when the camera is looking at the back-right side of the entity.")]
	[SerializeField] private Texture img_6_backright;
	[Tooltip("This is the image shown when the camera is looking at the right side of the entity.")]
	[SerializeField] private Texture img_7_right;
	[Tooltip("This is the image shown when the camera is looking at the front-right side of the entity.")]
	[SerializeField] private Texture img_8_frontright;
	
	[Header("Exposed values, not settings")]
	public bool Spinning = true;
	[SerializeField] private float facingDirAngle;
	[SerializeField] private float angleBetween;
	[SerializeField] private int spriteIndex;
	[SerializeField] Vector3 toPlayerVec;
	//[SerializeField] Vector3 facingDirVec;
	[SerializeField] Vector3 tmp;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	protected override void Start()
	{
		base.Start();

		if(eightSidedBillboard == null)
		{  Debug.LogError($"Error: {this} has no ({nameof(eightSidedBillboard)}) assigned in inspector!");  }

		// Get initial facing direction
		//facingDirVec = entitydirectionArrow.forward;
		//facingDirVec.y = 0.0f; // flatten vector

#if UNITY_EDITOR
		// Since the code modifies the material directly, it persists after exiting play mode.
		//   Save the asset's scale and offset, to restore when exiting play mode.
		// This is mainly because Git picks up every time this file changes.
		ogMainTex = eightSidedBillboard.material.mainTexture;
#endif
	}

#if UNITY_EDITOR
	private Texture ogMainTex;
	protected void OnDestroy()
	{
		// Restore asset's texture.
		eightSidedBillboard.material.mainTexture = ogMainTex;
	}
#endif

	// Update is called once per frame
	protected override void Update()
	{
		base.Update();

		if(Spinning)
		{
			Spin();
		}

		UpdateSprite();
	}

	private void Spin()
	{
		// Update facing direction of the entity
		facingDirAngle += rotSpeed * Time.deltaTime;
		// wrap the angle to [0, 360) boundary, "[" is inclusive, "(" is exclusive.
		if(facingDirAngle < 0.0f) { facingDirAngle += 360.0f; }
		if(facingDirAngle > 360.0f) { facingDirAngle -= 360.0f; }

		// Update arrow rotation
		tmp = entitydirectionArrow.transform.eulerAngles;
		tmp.y = facingDirAngle;
		entitydirectionArrow.transform.eulerAngles = tmp;
		//facingDirVec = entitydirectionArrow.transform.forward;
	}

	private void UpdateSprite()
	{
		// Can't update sprite if there's no player
		if(!PlayerControl.InstanceExists()) { return; }

		// Update direction to the player camera
		toPlayerVec = (PlayerControl.Instance.PlayerCamera.transform.position - towardsPlayerArrow.position);
		toPlayerVec.y = 0.0f; // flatten vector
		toPlayerVec.Normalize(); // Vector length = 1
		towardsPlayerArrow.forward = toPlayerVec;

		// Get the angle between the facing direction and the camera direction. Subtract from 360
		//   degrees to make it clockwise instead of counterclockwise (to match DOOM's angling).
		angleBetween = 360.0f - (towardsPlayerArrow.eulerAngles.y - facingDirAngle);

		// Scale the angle to be from 0 to 7 (360 / 45 = 8), then round to the nearest integer.
		// Rounding to the nearest int makes every angle pick a sprite index - all
		//   numbers from 3.5 to 4.49 are closest to 4, which will be the sprite
		//   index shown; note that 4 itself is in the middle of this range).
		spriteIndex = Mathf.RoundToInt(angleBetween / 45.0f);
		// Since anything above 7.5 rounds to 8, but it actually belongs to the first sector, so wrap it around.
		spriteIndex %= 8;
		// Make the range 1 to 8 (this is largely unnecessary, but is done to align with DOOM's numbering)
		spriteIndex++;

		eightSidedBillboard.material.mainTexture = spriteIndex switch
		{
			8 => img_8_frontright,
			7 => img_7_right,
			6 => img_6_backright,
			5 => img_5_back,
			4 => img_4_backleft,
			3 => img_3_left,
			2 => img_2_frontleft,
			_ => img_1_front, // default to the front image
		};
	}


#if UNITY_EDITOR // Gizmos don't exist outside of the editor, so just remove from binaries entirely
	private void OnDrawGizmos()
	{
		// Don't run outside of editor play mode.
		if(!UnityEditor.EditorApplication.isPlaying) { return; }

		// Draw arrows as gizmos
		Gizmos.color = Color.red;
		Gizmos.DrawRay(entitydirectionArrow.position, entitydirectionArrow.transform.forward);
		Gizmos.color = Color.blue;
		Gizmos.DrawRay(towardsPlayerArrow.position, toPlayerVec);
	}
#endif
}
