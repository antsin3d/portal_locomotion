using UnityEngine;

namespace Teleportals {
    public class TeleportUser_E_Key : MonoBehaviour
    {
        public GameObject teleportTarget;
        public float teleportDelay = 1f;
        public GameObject objectToHide1;
        public GameObject objectToHide2;
        private float teleportTime;

        void Update()
        {
            if (Input.GetKeyDown("e"))
            {
                if (Time.time >= teleportTime)
                {
                    // Teleport the player character to the teleportTarget object
                    transform.position = teleportTarget.transform.position;
                    
                    // Hide the two objects
                    objectToHide1.SetActive(false);
                    objectToHide2.SetActive(false);
                    
                    // Set the teleportTime variable to the current time plus the teleport delay
                    teleportTime = Time.time + teleportDelay;
                }
            }
        }
    }
}