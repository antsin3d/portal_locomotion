using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine;

public class RightHandRaycast : MonoBehaviour
{
	public Transform playerCamera;
	public Transform playerCameraR;
	public Transform playerCameraCenter;
	public Transform portalCameraL;
	public Transform portalCameraR;
	public Transform portalParentL;
	public Transform portalParentR;
    private Quaternion portalCamInitialRotationL;
    private Quaternion portalCamInitialRotationR;
	
	public Transform player;
    public Transform rightHand;
    public Transform portalCameraParent;
    public Transform objectToAnimateL;
	public Transform objectToAnimateR;
    public GameObject portalMeshL;
    public GameObject portalMeshR;
	public GameObject portalMeshStencilL;
	public GameObject portalMeshStencilR;
	public Transform portalMeshStencilLtransform;
	public Transform portalMeshStencilRtransform;
	
	public float portalDiscDistance = 0.3f;
    private Vector3 startScale;
    public Vector3 endScale;
    private float animationDuration;
	private float animationDurationClose;
    private float animationDurationMid;
	private float animationDurationFar;
	//set how far the user is allowed to teleport by default
    public float maxDistance = 100f;
	
    public LayerMask layerMask;
    private Vector3 hitPoint;
	private Vector3 hitPointLock;
	private Quaternion initialPortalParentRotationL;
	private Quaternion initialPortalParentRotationR;
	private Vector3 initialPortalParentPositionL;
	private Vector3 initialPortalParentPositionR;
	private Quaternion portalRotationLockL;
	private Quaternion portalRotationLockR;
	private Quaternion portalStartRotation;
	
    private bool isAnimating = false;
	float teleportDistance;
	private string hmdName;
	public enum SpeedOption
    {
        Slow,
        Default,
        Quick
    }
	public SpeedOption portalSpeed = SpeedOption.Default;
	//this will be updated as we add support for more headsets - let us know if you have values for us!
	public enum Headset
    {
        Meta_Quest_2
    }
	public Headset portalQuadSize;
	private float portalQuadWidth;
	private float portalQuadHeight;
	
	private Vector3 leftCameraPosition;
	private Vector3 rightCameraPosition;
	
	private Transform leftCameraTransform;
	private Transform rightCameraTransform;
	 
	private float quadSizeSet;
	float playerFOV;
	Camera playerCameraComponentL;
	Camera portalCameraComponentL;
	Camera portalCameraComponentR;
	
private void Start()
    {
		SetSpeedValues();
		setPortalQuadSize();
		//save spawned portal camera rotation
        portalCamInitialRotationL = portalCameraL.transform.rotation;
        portalCamInitialRotationR = portalCameraR.transform.rotation;
		portalStartRotation = portalParentL.localRotation;
		initialPortalParentRotationL = portalParentL.rotation;
		initialPortalParentRotationR = portalParentR.rotation;
		initialPortalParentPositionL = portalParentL.position;
		initialPortalParentPositionR = portalParentR.position;
		//get the cameras for FOV setting
		playerCameraComponentL = playerCamera.GetComponent<Camera>();
		portalCameraComponentL = portalCameraL.GetComponent<Camera>();
		portalCameraComponentR = portalCameraR.GetComponent<Camera>();

    }
	//Configure your movement speed at various distances
    private void SetSpeedValues()
    {
        switch (portalSpeed)
        {
            case SpeedOption.Slow:
                animationDurationClose = 0.8f;
                animationDurationMid = 1f;
                animationDurationFar = 1.3f;
                break;
            case SpeedOption.Default:
                animationDurationClose = 0.5f;
                animationDurationMid = 0.7f;
                animationDurationFar = 1f;
                break;
            case SpeedOption.Quick:
                animationDurationClose = 0.3f;
                animationDurationMid = 0.5f;
                animationDurationFar = 0.7f;
                break;
        }
	}
	//once we have multiple headsets supported we'll do this based on pulled headset name but this works for now
	//if you have tips on pulling the name let me know. So far I only get things like "Head Tracking - OpenXR"
	private void setPortalQuadSize()
    {
		switch (portalQuadSize) {
			case Headset.Meta_Quest_2:
				portalQuadWidth = 4.48f;
				portalQuadHeight = 2.396413f;
				break;
		}
	}

    private void Update()
    {
		
		//checking the *actual* offset of the player cameras
		leftCameraPosition = InputTracking.GetLocalPosition(XRNode.LeftEye);
		rightCameraPosition = InputTracking.GetLocalPosition(XRNode.RightEye);
		
		//ensure the raycast only hits world objects and not the portal objects
		int layerMask = 1 << LayerMask.NameToLayer("TestLayer");
		RaycastHit hit;

		// // Attach the following 4 lines of code to the objects themselves if you want to optimize. Left here for clarity
		//have spawned cameras follow the relative position and rotation of the playercameras
		portalCameraL.transform.localPosition = leftCameraPosition;
		portalCameraL.transform.localRotation = playerCamera.localRotation;
		portalCameraR.transform.localPosition = rightCameraPosition;
		portalCameraR.transform.localRotation = playerCameraR.localRotation;
		
		// Calculate the midpoint between the two cameras
		Vector3 midpoint = (playerCamera.position + playerCameraR.position) / 2;
		
		// Set the position of the object to the midpoint between the two player cameras
		playerCameraCenter.position = midpoint;
		// // if isanimating return
		 if (Physics.Raycast(rightHand.position, rightHand.forward, out hit, maxDistance, layerMask))
        {
           //before pressing 'fire' key:
			 if (isAnimating == false)
            {
			hitPoint = hit.point;
            portalCameraParent.position = hitPoint;
			
			 // Calculate the direction from the camera to the hitpoint to position mask objects starting position
            Vector3 direction = hitPoint - playerCamera.position;
			Vector3 directionR = hitPoint - playerCameraR.position;
			Vector3 camToHit = hitPoint - playerCamera.position;
			Vector3 objectPos = playerCamera.position + camToHit.normalized * portalDiscDistance;
			portalParentL.position = objectPos;
			portalParentL.LookAt(hitPoint);
			
			Vector3 camToHitR = hitPoint - playerCameraR.position;
			Vector3 objectPosR = playerCameraR.position + camToHit.normalized * portalDiscDistance;
			portalParentR.position = objectPosR;
			portalParentR.LookAt(hitPoint);
			
			//rotate the camera center to stay in line with the two cameras
			Quaternion rotation = Quaternion.Lerp(playerCamera.rotation, playerCameraR.rotation, 0.5f);
			playerCameraCenter.rotation = rotation;
			
			//copy the z-axis rotation of playerCameraCenter to portalParentL 
            Quaternion currentRotation = portalParentL.rotation;
            Quaternion targetRotation = playerCameraCenter.rotation;
            portalParentL.rotation = Quaternion.Euler(currentRotation.eulerAngles.x, currentRotation.eulerAngles.y, targetRotation.eulerAngles.z);

            Quaternion currentRotationR = portalParentR.rotation;
            Quaternion targetRotationR = playerCameraCenter.rotation;
            portalParentR.rotation = Quaternion.Euler(currentRotationR.eulerAngles.x, currentRotationR.eulerAngles.y, targetRotationR.eulerAngles.z);
			//set portal cameras FOV to match player cameras
			if (portalCameraComponentL.fieldOfView == 60f) {
				playerFOV = playerCameraComponentL.fieldOfView;
				portalCameraComponentL.fieldOfView = playerFOV;
				portalCameraComponentR.fieldOfView = playerFOV;
				//hmdName = SystemInfo.deviceName;
				//Debug.Log(hmdName);
				}
			}
        }
		
        
		if (Input.GetButtonDown("Fire1")) {
			if (isAnimating == false)
			{
				//set initial locations
				isAnimating = true;
				hitPointLock = hitPoint;
				portalCameraParent.position = hitPointLock;
				portalRotationLockL = portalParentL.localRotation;
				portalRotationLockR = portalParentR.localRotation;
				//in the future I'll derive these distance values as a % of your max distance
				teleportDistance = Vector3.Distance(player.position, hitPoint);
				if (teleportDistance > 10){
					animationDuration = animationDurationMid;
				}
				if (teleportDistance > 20) {
					animationDuration = animationDurationFar;
				}
				if (teleportDistance < 10) {
					animationDuration = animationDurationClose;
				}
				//show portal objects
				portalMeshL.SetActive(true);
				portalMeshR.SetActive(true);
				portalMeshStencilL.SetActive(true);
				portalMeshStencilR.SetActive(true);
				StartCoroutine(AnimateObject());
			}
        }
    }

    IEnumerator AnimateObject()
    {
		if (animationDuration <= 0)
    {
        Debug.LogError("Animation duration must be greater than 0");
        yield break;
    }
        float startTime = Time.time;
        while (Time.time < startTime + animationDuration)
        {
            float timePassed = Time.time - startTime;
			float proportionComplete = timePassed / animationDuration;
            objectToAnimateL.localScale = Vector3.Lerp(startScale, endScale, proportionComplete);
			objectToAnimateR.localScale = Vector3.Lerp(startScale, endScale, proportionComplete);
			portalParentL.localRotation = Quaternion.Lerp(portalRotationLockL, portalStartRotation, proportionComplete);
			portalParentR.localRotation = Quaternion.Lerp(portalRotationLockR, portalStartRotation, proportionComplete);
            yield return null;
        }
        objectToAnimateL.localScale = endScale;
		objectToAnimateR.localScale = endScale;
		portalParentL.rotation = portalStartRotation;
		portalParentR.rotation = portalStartRotation;
        TeleportPlayer();
    }
	
	
    private void TeleportPlayer()
    {
        player.position = hitPointLock;
		portalMeshStencilL.SetActive(false);
		portalMeshStencilR.SetActive(false);
		portalMeshL.SetActive(false);
		portalMeshR.SetActive(false);
		isAnimating = false;
    }
}