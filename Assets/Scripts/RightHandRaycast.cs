using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine;
using UnityEngine.UI;

public class RightHandRaycast : MonoBehaviour
{
	public Transform playerCamera;
	public Transform playerCameraR;
	public Transform playerCameraCenter;
    public Transform portalCameraParent;
	public Transform portalCameraL;
	public Transform portalCameraR;
	public Transform portalParentL;
	public Transform portalParentR;
    private Quaternion portalCamInitialRotationL;
    private Quaternion portalCamInitialRotationR;
	public Transform player;
    public Transform rightHand;
    public Transform objectToAnimateL;
	public Transform objectToAnimateR;
    public GameObject portalMeshL;
    public GameObject portalMeshR;
	public GameObject portalMeshStencilL;
	public GameObject portalMeshStencilR;
	public Transform portalMeshStencilLtransform;
	public Transform portalMeshStencilRtransform;
	//how far from the camera the stencil is, the start/end scales of it, and teh animation duration for various distances of movement
	private float portalDiscDistance = 0.3f;
	private Vector3 portalDiscStartWidth;
	private Vector3 portalDiscStartHeight;
	private Vector3 portalDiscEndWidth;
	private Vector3 portalDiscEndHeight;
    private Vector3 startScale;
    
    private float animationDuration;
	private float animationDurationClose;
    private float animationDurationMid;
	private float animationDurationFar;
	//set how far the user is allowed to teleport by default
    public float maxDistance = 30f;
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
	private float portalQuadWidth;
	private float portalQuadHeight;
	
	private Vector3 leftCameraPosition;
	private Vector3 rightCameraPosition;
	private Quaternion leftCameraRotation;
	private Quaternion rightCameraRotation;
	
	private Transform leftCameraTransform;
	private Transform rightCameraTransform;
	 
	//private float quadSizeSet;
	float playerFOVL;
	float playerFOVR;
	Camera playerCameraComponentL;
	Camera playerCameraComponentR;
	Camera portalCameraComponentL;
	Camera portalCameraComponentR;

	//is vr headset loaded?
	private bool vrDeviceLoaded = false;

	// Set the distance from the camera to the quad
	float portalQuadsDistance = 1f;
	
	public RenderTexture renderTextureL;
    public RenderTexture renderTextureR;
	public int renderTextureWidth;
	public int renderTextureHeight;
	private bool hasCreatedRenderTexture = false;
	float finalDiscWidth;
	float finalDiscHeight;
	float scaleFactor;
	float scaleFactorWidth;
	float scaleFactorHeight;
private void Start()
    {
		SetSpeedValues();
		StartCoroutine(CheckForVRDevice());
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
		playerCameraComponentR = playerCameraR.GetComponent<Camera>();
		portalCameraComponentL = portalCameraL.GetComponent<Camera>();
		portalCameraComponentR = portalCameraR.GetComponent<Camera>();
    }

	void CreateAndAssignRenderTexture()
    {
        int renderTextureWidth = XRSettings.eyeTextureWidth * 2;
        int renderTextureHeight = XRSettings.eyeTextureHeight;

		
        // Create the RenderTexture with the specified dimensions
        renderTextureL = new RenderTexture(renderTextureWidth, renderTextureHeight, 24, RenderTextureFormat.ARGB32);
        renderTextureL.Create();
		renderTextureR = new RenderTexture(renderTextureWidth, renderTextureHeight, 24, RenderTextureFormat.ARGB32);
        renderTextureR.Create();

		// Get the Camera component from the portalCameraL game object
    	Camera portalCameraLComponent = portalCameraL.GetComponent<Camera>();
		Camera portalCameraRComponent = portalCameraR.GetComponent<Camera>();

        // Set the RenderTexture as the target texture for the portalCameraL
		if (portalCameraLComponent != null)
		{
			portalCameraLComponent.targetTexture = renderTextureL;
			portalCameraRComponent.targetTexture = renderTextureR;
		}
		else
		{
			Debug.LogError("Camera component not found on portalCamera.");
			return;
		}

        // Assign the RenderTexture to the Albedo (RGB) texture channel of the portalMesh
        Renderer objectRendererL = portalMeshL.GetComponent<Renderer>();
		Renderer objectRendererR = portalMeshR.GetComponent<Renderer>();
        if (objectRendererL != null && objectRendererL.material != null)
        {
            objectRendererL.material.SetTexture("_MainTex", renderTextureL);
			objectRendererR.material.SetTexture("_MainTex", renderTextureR);
        }
        else
        {
            Debug.LogError("Renderer or material not found on portalMesh.");
        }
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

	private IEnumerator CheckForVRDevice()
    {
        // Wait until the VR device is active
        while (!XRSettings.isDeviceActive)
        {
            yield return null;
        }

        vrDeviceLoaded = true;
    }

    private void Update()
    {
			if (!hasCreatedRenderTexture && XRSettings.eyeTextureWidth != 0)
			{
				CreateAndAssignRenderTexture();
				hasCreatedRenderTexture = true;
			}
			//checking the *actual* offset of the player cameras
			leftCameraPosition = InputTracking.GetLocalPosition(XRNode.LeftEye);
			rightCameraPosition = InputTracking.GetLocalPosition(XRNode.RightEye);
			leftCameraRotation = InputTracking.GetLocalRotation(XRNode.LeftEye);
			rightCameraRotation = InputTracking.GetLocalRotation(XRNode.RightEye);

			// // Attach the following 4 lines of code to the objects themselves if you want to optimize. Left here for learning purposes
			//have spawned cameras follow the relative position and rotation of the playercameras
			portalCameraL.transform.localPosition = leftCameraPosition;
			portalCameraL.transform.localRotation = playerCamera.localRotation;
			portalCameraR.transform.localPosition = rightCameraPosition;
			portalCameraR.transform.localRotation = playerCameraR.localRotation;

			//position portal quads
			Vector3 portalQuadForwardOffsetL = leftCameraRotation * Vector3.forward;
			Vector3 newPortalQuadPositionL = leftCameraPosition + portalQuadForwardOffsetL * portalQuadsDistance;
			portalMeshL.transform.localPosition = newPortalQuadPositionL;
			Vector3 portalQuadForwardOffsetR = rightCameraRotation * Vector3.forward;
			Vector3 newPortalQuadPositionR = rightCameraPosition + portalQuadForwardOffsetR * portalQuadsDistance;
			portalMeshR.transform.localPosition = newPortalQuadPositionR;

			portalMeshL.transform.localRotation = leftCameraRotation;
			portalMeshR.transform.localRotation = rightCameraRotation;

			List<XRNodeState> nodeStates = new List<XRNodeState>();
			InputTracking.GetNodeStates(nodeStates);

			//Debug.Log("Connected VR headset: " + SystemInfo.deviceModel);
			Debug.Log("Eye Texture Width: " + XRSettings.eyeTextureWidth);
			Debug.Log("Eye Texture Height: " + XRSettings.eyeTextureHeight);
			float renderScale = XRSettings.eyeTextureResolutionScale;
			playerFOVL = playerCameraComponentL.fieldOfView;
			playerFOVR = playerCameraComponentR.fieldOfView;
			//Debug.Log("Vertical FOV: " + playerFOVL);
			portalCameraComponentL.fieldOfView = playerFOVL;
			portalCameraComponentR.fieldOfView = playerFOVR;
			float aspectRatioL = playerCameraComponentL.aspect;
			float aspectRatioR = playerCameraComponentR.aspect;
			//Debug.Log("Aspect Ratio: " + aspectRatioL.ToString("F5"));
			float horizontalFOVL = 2 * Mathf.Atan(Mathf.Tan(playerFOVL * Mathf.Deg2Rad / 2) * aspectRatioL) * Mathf.Rad2Deg;
			float horizontalFOVR = 2 * Mathf.Atan(Mathf.Tan(playerFOVR * Mathf.Deg2Rad / 2) * aspectRatioR) * Mathf.Rad2Deg;
			Debug.Log("Left eye Local Position unity: " + playerCamera.localPosition.ToString("F5"));
			//Debug.Log("Right eye Local Position unity: " + playerCameraR.localPosition.ToString("F5"));
			//Debug.Log("Left eye Local Position XR: " + leftCameraPosition.ToString("F5"));
			//Debug.Log("Right eye Local Position XR: " + rightCameraPosition.ToString("F5"));
			//Debug.Log("Left Portal Camera Local Position: " + portalCameraL.transform.localPosition.ToString("F5"));
			Debug.Log("Right Portal Camera Local Position: " + portalCameraR.transform.localPosition.ToString("F5"));

			float frustumHeightL = 2.0f * Mathf.Tan(playerFOVL * 0.5f * Mathf.Deg2Rad);
			float frustumWidthL = frustumHeightL * aspectRatioL;
			//Debug.Log("Frustum size at 1m distance: Width = " + frustumWidthL.ToString("F5") + ", Height = " + frustumHeightL.ToString("F5"));
			float frustumHeightR = 2.0f * Mathf.Tan(playerFOVR * 0.5f * Mathf.Deg2Rad);
			float frustumWidthR = frustumHeightR * aspectRatioR;

			Vector3 newScaleL = new Vector3(2f * frustumWidthL, frustumHeightL, 1f);
			portalMeshL.transform.localScale = newScaleL;
			Vector3 newScaleR = new Vector3(2f * frustumWidthR, frustumHeightR, 1f);
			portalMeshR.transform.localScale = newScaleR;

			// Calculate the frustum height at the given distance
			float frustumHeight = 2.0f * portalDiscDistance * Mathf.Tan(playerFOVL * 0.5f * Mathf.Deg2Rad);

			// Calculate the frustum width at the given distance
			float frustumWidth = frustumHeight * aspectRatioL;

			// Divide the frustum width and height by the disc's dimensions (1m x 1m)
			scaleFactorWidth = frustumWidth / 1f;
			scaleFactorHeight = frustumHeight / 1f;

			// Calculate the screen's diagonal length
			scaleFactor = Mathf.Sqrt(XRSettings.eyeTextureWidth * XRSettings.eyeTextureWidth + XRSettings.eyeTextureHeight * XRSettings.eyeTextureHeight);
			Debug.Log("Scale Factor: " + scaleFactor);
			// Determine the final width and height of the disc
			finalDiscWidth = scaleFactor * aspectRatioL;
			finalDiscHeight = scaleFactor;
			Debug.Log("Final Disc Width: " + finalDiscWidth);
			Debug.Log("Final Disc Height: " + finalDiscHeight);
			

			//ensure the raycast only hits world objects and not the portal objects
			int layerMask = 1 << LayerMask.NameToLayer("TestLayer");
			RaycastHit hit;

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
        Vector3 endScale = new Vector3(scaleFactorWidth, scaleFactorHeight, 1f);

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