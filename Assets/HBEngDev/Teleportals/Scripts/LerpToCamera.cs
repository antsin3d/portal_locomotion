using UnityEngine;
namespace Teleportals {
    public class LerpToCamera : MonoBehaviour
    {
        public GameObject portalQuad;
        public GameObject portalParent;
        public Transform player;
        public Camera portalCamera;
        public Transform playerCamera;

        public float animationSpeed = 1.0f;
        public float startSize = 0.1f;
        public float endSize = 1.0f;
        
        private Vector3 currentPosition;
        private bool isAnimating = false;
        private Vector3 newPosition;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                isAnimating = true;
                //startSize = 0.1;
                currentPosition = portalQuad.transform.localPosition;
                portalQuad.SetActive(true);
            }
            if (isAnimating)
            {
                startSize += animationSpeed * Time.deltaTime;
                portalQuad.transform.localScale = new Vector3(startSize, startSize, startSize);
                //portalQuad.transform.localPosition = Vector3.Lerp(currentPosition, Vector3.zero, startSize);
                //newPosition = portalCamera.transform.position;
                if (startSize >= endSize)
                {
                    isAnimating = false;
                    portalQuad.SetActive(false);
                    player.position = portalParent.transform.position;
                }
            }
        }
    }
}