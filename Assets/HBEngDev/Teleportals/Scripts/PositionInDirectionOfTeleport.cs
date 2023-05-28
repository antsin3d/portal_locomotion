using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Teleportals {
    public class PositionInDirectionOfTeleport : MonoBehaviour
    {
        public Transform objectToPlace; // The object you want to place along the raycast
        public Transform playerCamera; // The player's camera
        public float distanceFromCamera; // The distance you want the object to be placed from the camera
        public Transform rightController; // The right controller object

        void Update()
        {
            // Raycast from the right controller
            RaycastHit hit;
            if (Physics.Raycast(rightController.position, rightController.forward, out hit))
            {
                // Calculate the position of the object to place
                Vector3 position = playerCamera.position;
                position.z = hit.point.z - distanceFromCamera;
                objectToPlace.position = position;
            }
        }
    }
}