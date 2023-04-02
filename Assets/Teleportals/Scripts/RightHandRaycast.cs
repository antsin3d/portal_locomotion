using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine;
using UnityEngine.UI;
namespace teleportals
{
	public enum SpeedOption
		{
			Slow,
			Default,
			Quick
		}
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
		public Transform cameraOffsetTransform;
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
		private Vector3 newObjectToAnimatePositionL;
		private Vector3 newObjectToAnimatePositionR;
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
		private Quaternion portalStartRotationL;
		private Quaternion portalStartRotationR;
		
		private bool isAnimating = false;
		private float teleportDistance;
		
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
		private float portalQuadsDistance = 1f;
		
		public RenderTexture renderTextureL;
		public RenderTexture renderTextureR;
		private int renderTextureWidth;
		private int renderTextureHeight;
		private bool hasCreatedRenderTexture = false;
		float finalDiscWidth;
		float finalDiscHeight;
		float scaleFactor;
		float scaleFactorWidth;
		float scaleFactorHeight;
		private Transform leftEyeParent;
		private Transform rightEyeParent;
		private Vector3 portalStartPosL;
		private Quaternion portalStartRotL;
		private Vector3 portalStartPosR;
		private Quaternion portalStartRotR;

		private void Start()
			{
				SetSpeedValues();
				StartCoroutine(CheckForVRDevice());
				//save spawned portal camera rotation
				portalCamInitialRotationL = portalCameraL.transform.rotation;
				portalCamInitialRotationR = portalCameraR.transform.rotation;
				portalStartRotationL = portalParentL.rotation;
				portalStartRotationR = portalParentR.rotation;
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
					animationDurationClose = 0.9f;
					animationDurationMid = 1.2f;
					animationDurationFar = 1.5f;
					break;
				case SpeedOption.Default:
					animationDurationClose = 0.6f;
					animationDurationMid = 0.8f;
					animationDurationFar = 1.1f;
					break;
				case SpeedOption.Quick:
					animationDurationClose = 0.4f;
					animationDurationMid = 0.6f;
					animationDurationFar = 0.8f;
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
				//checking the *actual* local offset of the player cameras
				leftCameraPosition = InputTracking.GetLocalPosition(XRNode.LeftEye);
				//Debug.Log(leftCameraPosition);
				rightCameraPosition = InputTracking.GetLocalPosition(XRNode.RightEye);
				//Debug.Log(rightCameraPosition);
				leftCameraRotation = InputTracking.GetLocalRotation(XRNode.LeftEye);
				rightCameraRotation = InputTracking.GetLocalRotation(XRNode.RightEye);

				// Get world position and rotation of XRNode camera
				Vector3 leftCameraWorldPosition = cameraOffsetTransform.TransformPoint(leftCameraPosition);
				Quaternion leftCameraWorldRotation = cameraOffsetTransform.rotation * leftCameraRotation;
				Vector3 rightCameraWorldPosition = cameraOffsetTransform.TransformPoint(rightCameraPosition);
				Quaternion rightCameraWorldRotation = cameraOffsetTransform.rotation * rightCameraRotation;

				//have spawned cameras follow the relative position and rotation of the playercameras
				portalCameraL.transform.localPosition = leftCameraPosition;
				portalCameraL.transform.localRotation = leftCameraRotation;
				portalCameraR.transform.localPosition = rightCameraPosition;
				portalCameraR.transform.localRotation = rightCameraRotation;

				// Update Main Camera position and rotation to match XRNode
				playerCamera.transform.localPosition = leftCameraPosition;
				playerCamera.transform.localRotation = leftCameraRotation;
				playerCameraR.transform.localPosition = rightCameraPosition;
				playerCameraR.transform.localRotation = rightCameraRotation;

				//position portal quads
				Vector3 portalQuadForwardOffsetL = leftCameraRotation * Vector3.forward;
				Vector3 newPortalQuadPositionL = leftCameraPosition + portalQuadForwardOffsetL * portalQuadsDistance;
				portalMeshL.transform.localPosition = newPortalQuadPositionL;
				portalMeshL.transform.localRotation = leftCameraRotation;

				Vector3 portalQuadForwardOffsetR = rightCameraRotation * Vector3.forward;
				Vector3 newPortalQuadPositionR = rightCameraPosition + portalQuadForwardOffsetR * portalQuadsDistance;
				portalMeshR.transform.localPosition = newPortalQuadPositionR;
				portalMeshR.transform.localRotation = rightCameraRotation;
				///////Set up scaling for portals based on headset
				List<XRNodeState> nodeStates = new List<XRNodeState>();
				InputTracking.GetNodeStates(nodeStates);
				float renderScale = XRSettings.eyeTextureResolutionScale;
				playerFOVL = playerCameraComponentL.fieldOfView;
				playerFOVR = playerCameraComponentR.fieldOfView;
				portalCameraComponentL.fieldOfView = playerFOVL;
				portalCameraComponentR.fieldOfView = playerFOVR;
				float aspectRatioL = playerCameraComponentL.aspect;
				float aspectRatioR = playerCameraComponentR.aspect;
				float horizontalFOVL = 2 * Mathf.Atan(Mathf.Tan(playerFOVL * Mathf.Deg2Rad / 2) * aspectRatioL) * Mathf.Rad2Deg;
				float horizontalFOVR = 2 * Mathf.Atan(Mathf.Tan(playerFOVR * Mathf.Deg2Rad / 2) * aspectRatioR) * Mathf.Rad2Deg;
				float frustumHeightL = 2.0f * Mathf.Tan(playerFOVL * 0.5f * Mathf.Deg2Rad);
				float frustumWidthL = frustumHeightL * aspectRatioL;
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
				// Determine the final width and height of the disc
				finalDiscWidth = scaleFactor * aspectRatioL;
				finalDiscHeight = scaleFactor;				

				//ensure the raycast only hits world objects and not the portal objects
				int layerMask = 1 << LayerMask.NameToLayer("TestLayer");
				RaycastHit hit;

				// Calculate the midpoint between the two cameras
				Vector3 midpoint = (playerCamera.position + playerCameraR.position) / 2;
				// Set the position of the object to the midpoint between the two player cameras
				playerCameraCenter.position = midpoint;

			if (Physics.Raycast(rightHand.position, rightHand.forward, out hit, maxDistance, layerMask))
				{
					//before pressing 'fire' key:
					if (isAnimating == false)
					{
						hitPoint = hit.point;
						portalCameraParent.position = hitPoint;
						Vector3 direction = hitPoint - playerCamera.position;
						Vector3 directionR = hitPoint - playerCameraR.position;
						Vector3 camToHit = hitPoint - playerCamera.position;
						Vector3 objectPos = playerCamera.position + camToHit.normalized * portalDiscDistance;
						Vector3 objectPosR = playerCameraR.position + camToHit.normalized * portalDiscDistance;
						// position values to use in animation
						portalStartPosL = objectPos;
						portalStartPosR = objectPosR;
						// rotation values to use in animation
						Quaternion lookAtRotationL = Quaternion.LookRotation(hitPoint - objectPos, Vector3.up);
						Quaternion lookAtRotationR = Quaternion.LookRotation(hitPoint - objectPosR, Vector3.up);
						portalStartRotL = Quaternion.LookRotation(lookAtRotationL * -Vector3.forward, lookAtRotationL * Vector3.up);
						portalStartRotR = Quaternion.LookRotation(lookAtRotationR * -Vector3.forward, lookAtRotationR * Vector3.up);
					}
				}
			
			
			if (Input.GetButtonDown("Fire1")) {
				if (isAnimating == false)
				{
					//set initial locations
					isAnimating = true;
					hitPointLock = hitPoint;
					portalCameraParent.position = hitPointLock;
					portalRotationLockL = portalParentL.rotation;
					portalRotationLockR = portalParentR.rotation;

					//in the future I'll derive these distance values as a % of your max distance
					teleportDistance = Vector3.Distance(player.position, hitPoint);
					if (teleportDistance > maxDistance*0.33f){
						animationDuration = animationDurationMid;
					}
					if (teleportDistance > maxDistance*0.66f) {
						animationDuration = animationDurationFar;
					}
					if (teleportDistance < maxDistance*0.33f) {
						animationDuration = animationDurationClose;
					}
					//show portal objects
					portalMeshL.SetActive(true);
					portalMeshR.SetActive(true);
					portalMeshStencilL.SetActive(true);
					portalMeshStencilR.SetActive(true);
					//Debug.Log("portal centering on fire" + newObjectToAnimatePositionL);
					StartCoroutine(AnimateObject());
				}
			}
		}

		IEnumerator AnimateObject()
		{
			isAnimating = true;
			if (animationDuration <= 0)
			{
				Debug.LogError("Animation duration must be greater than 0");
				yield break;
			}
			Vector3 endScale = new Vector3(scaleFactorWidth, scaleFactorHeight, 1f);

			float startTime = Time.time;
			while (Time.time < startTime + animationDuration)
			{
				//update final positioning of disc at end of animation
				Vector3 objectToAnimateForwardOffsetL = playerCamera.transform.forward;
				newObjectToAnimatePositionL = playerCamera.transform.position + objectToAnimateForwardOffsetL * portalDiscDistance;
				Quaternion tempRotationL = playerCamera.transform.rotation;

				Vector3 objectToAnimateForwardOffsetR = playerCameraR.transform.forward;
				newObjectToAnimatePositionR = playerCameraR.transform.position + objectToAnimateForwardOffsetR * portalDiscDistance;
				Quaternion tempRotationR = playerCameraR.transform.rotation;
				// Flip the forward direction of portalParentL by negating the forward vector
				portalStartRotationL = Quaternion.LookRotation(-playerCamera.transform.forward, playerCamera.transform.up);
				portalStartRotationR = Quaternion.LookRotation(-playerCameraR.transform.forward, playerCameraR.transform.up);
				float timePassed = Time.time - startTime;
				float proportionComplete = timePassed / animationDuration;

				objectToAnimateL.localScale = Vector3.Lerp(startScale, endScale, proportionComplete);
				objectToAnimateR.localScale = Vector3.Lerp(startScale, endScale, proportionComplete);
				portalParentL.position = Vector3.Lerp(portalStartPosL, newObjectToAnimatePositionL, proportionComplete);
				portalParentL.rotation = Quaternion.Lerp(portalStartRotL, portalStartRotationL, proportionComplete);
				portalParentR.position = Vector3.Lerp(portalStartPosR, newObjectToAnimatePositionR, proportionComplete);
				portalParentR.rotation = Quaternion.Lerp(portalStartRotR, portalStartRotationR, proportionComplete);

				yield return null;
			}
			objectToAnimateL.localScale = endScale;
			objectToAnimateR.localScale = endScale;
			portalParentL.position = newObjectToAnimatePositionL;
			portalParentL.rotation = portalStartRotationL;
			portalParentR.position = newObjectToAnimatePositionR;
			portalParentR.rotation = portalStartRotationR;
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
}