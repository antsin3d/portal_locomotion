using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class portalcamera_matchmove : MonoBehaviour
{
    public Transform playerCamera;
	
	public Transform portalCameraL;
	public Transform portalCameraR;
	
    private Vector3 portalCamInitialPositionL;
    private Quaternion portalCamInitialRotationL;
	
	private Vector3 portalCamInitialPositionR;
    private Quaternion portalCamInitialRotationR;

    private void Start()
    {
        portalCamInitialPositionL = portalCameraL.transform.position;
        portalCamInitialRotationL = portalCameraL.transform.rotation;
		portalCamInitialPositionR = portalCameraR.transform.position;
        portalCamInitialRotationR = portalCameraR.transform.rotation;
    }

    private void LateUpdate()
    {
        portalCameraL.transform.position = portalCamInitialPositionL + playerCamera.position;
        portalCameraL.transform.rotation = playerCamera.rotation * portalCamInitialRotationL;
		portalCameraR.transform.position = portalCamInitialPositionR + playerCamera.position;
        portalCameraR.transform.rotation = playerCamera.rotation * portalCamInitialRotationR;
    }
}