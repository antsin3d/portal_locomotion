using System.Collections;
using System.Collections.Generic;
//using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine;
//using UnityEngine.XR.Interaction.Toolkit;

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
	
	public float distance = 0.1f;
    private Vector3 startScale;
    public Vector3 endScale;
    private float animationDuration;
	private float animationDurationClose;
    private float animationDurationMid;
	private float animationDurationFar;
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
	
	public Vector3 LCamOffset;
	public Vector3 RCamOffset;
	/*public float portalFadeDuration = 0.3f;
	private Material portalMeshLMaterial;
	private Material portalMeshRMaterial;
	private Color portalMeshLColor;
	private Color portalMeshRColor;*/
	
    private bool isAnimating = false;
	float teleportDistance;
	
	public enum SpeedOption
    {
        Slow,
        Default,
        Quick
    }
	public SpeedOption portalSpeed = SpeedOption.Default;
	
	private Vector3 leftCameraPosition;
	private Vector3 rightCameraPosition;
	
	private Transform leftCameraTransform;
	private Transform rightCameraTransform;
	
private void Start()
    {
		
		SetSpeedValues();
		//save spawned portal camera rotation
        portalCamInitialRotationL = portalCameraL.transform.rotation;
        portalCamInitialRotationR = portalCameraR.transform.rotation;
		portalStartRotation = portalParentL.localRotation;
		initialPortalParentRotationL = portalParentL.rotation;
		initialPortalParentRotationR = portalParentR.rotation;
		initialPortalParentPositionL = portalParentL.position;
		initialPortalParentPositionR = portalParentR.position;
		
		/*portalMeshLMaterial = portalMeshL.GetComponent<Renderer>().material;
		portalMeshRMaterial = portalMeshR.GetComponent<Renderer>().material;
		portalMeshLColor = portalMeshLMaterial.color;
		portalMeshRColor = portalMeshRMaterial.color;*/
    }

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

    private void Update()
    {
		
		leftCameraPosition = InputTracking.GetLocalPosition(XRNode.LeftEye);
		rightCameraPosition = InputTracking.GetLocalPosition(XRNode.RightEye);
		//leftCameraTransform = Camera.main.transform.parent.Find("LeftEyeAnchor");
		//rightCameraTransform = Camera.main.transform.parent.Find("RightEyeAnchor");
		Debug.Log("Left Eye Position: " + leftCameraPosition.ToString("F3"));
		Debug.Log("Left Eye Position: " + rightCameraPosition.ToString("F3"));
		Debug.Log("Distance: " + Vector3.Distance(leftCameraPosition, rightCameraPosition));
		//ensure the raycast only hits world objects and not the portal objects
		int layerMask = 1 << LayerMask.NameToLayer("TestLayer");
		RaycastHit hit;
		//have spawned cameras follow the relative position and rotation of the playercameras
		//portalCameraL.transform.localPosition = playerCamera.localPosition + LCamOffset;
		portalCameraL.transform.localPosition = leftCameraPosition;
		//Debug.Log("Left Portal Cam Position: " + portalCameraL.transform.localPosition);
		portalCameraL.transform.localRotation = playerCamera.localRotation;
		//portalCameraR.transform.localPosition = playerCameraR.localPosition + RCamOffset;
		portalCameraR.transform.localPosition = rightCameraPosition;
		portalCameraR.transform.localRotation = playerCameraR.localRotation;
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
			
			 // Calculate the direction from the camera to the hitpoint to position mask objects
            Vector3 direction = hitPoint - playerCamera.position;
			Vector3 directionR = hitPoint - playerCameraR.position;
			Vector3 camToHit = hitPoint - playerCamera.position;
			Vector3 objectPos = playerCamera.position + camToHit.normalized * distance;
			portalParentL.position = objectPos;
			portalParentL.LookAt(hitPoint);
			
			Vector3 camToHitR = hitPoint - playerCameraR.position;
			Vector3 objectPosR = playerCameraR.position + camToHit.normalized * distance;
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
		
        
			if (Input.GetButtonDown("Fire1"))
        {
            
            if (isAnimating == false)
            {
				
                isAnimating = true;
				hitPointLock = hitPoint;
				portalCameraParent.position = hitPointLock;
				portalRotationLockL = portalParentL.localRotation;
				portalRotationLockR = portalParentR.localRotation;
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
		//StartCoroutine(FadePortalMeshMaterials());
		portalMeshStencilL.SetActive(false);
		portalMeshStencilR.SetActive(false);
		portalMeshL.SetActive(false);
		portalMeshR.SetActive(false);
		isAnimating = false;
    }
	
	/*IEnumerator FadePortalMeshMaterials()
	{
		float currentFadeDuration = 0f;
		float fadeStartTime = Time.time;

		while (currentFadeDuration < portalFadeDuration)
		{
			currentFadeDuration = Time.time - fadeStartTime;
			float t = currentFadeDuration / portalFadeDuration;
			Color newColor = new Color(portalMeshLColor.r, portalMeshLColor.g, portalMeshLColor.b, Mathf.Lerp(1, 0, t));
			portalMeshLMaterial.color = newColor;
			portalMeshRMaterial.color = newColor;
			yield return null;
		}

		portalMeshStencilL.SetActive(false);
		portalMeshStencilR.SetActive(false);
		portalMeshL.SetActive(false);
		portalMeshR.SetActive(false);
		//portalMeshLMaterial.color = new Color(portalMeshLColor.r, portalMeshLColor.g, portalMeshLColor.b, 1f);
		//portalMeshRMaterial.color = new Color(portalMeshRColor.r, portalMeshRColor.g, portalMeshRColor.b, 1f);
		isAnimating = false;
	}*/
}