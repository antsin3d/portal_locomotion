using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine;
namespace Teleportals {
    public class AspectRatioScript : MonoBehaviour
    {
        void Start()
        {
            // Get the aspect ratio of the left and right eye displays
            //float aspectRatio = XRSettings.eyeTextureWidth / XRSettings.eyeTextureHeight;
            // Print the aspect ratio to the console
        // Debug.Log("Aspect Ratio: " + aspectRatio);
            //Debug.Log(aspectRatio);
            Debug.Log(UnityEngine.XR.XRSettings.renderViewportScale);
            Debug.Log(UnityEngine.Screen.width);
            Debug.Log(UnityEngine.Screen.height);
        }
    }
}