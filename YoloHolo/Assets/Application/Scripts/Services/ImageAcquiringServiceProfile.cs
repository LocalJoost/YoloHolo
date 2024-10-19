
using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.ServiceFramework.Interfaces;
using UnityEngine;

namespace YoloHolo.Services
{
    [CreateAssetMenu(menuName = "ImageAcquiringServiceProfile", fileName = "ImageAcquiringServiceProfile",
        order = (int)CreateProfileMenuItemIndices.ServiceConfig)]
    public class ImageAcquiringServiceProfile : BaseServiceProfile<IServiceModule>
    {
        [SerializeField]
        private int cameraFPS = 4;

        [SerializeField]
        private Vector2Int requestedCameraSize = new(896, 504);

        public int CameraFPS => cameraFPS;
        
        public Vector2Int RequestedCameraSize => requestedCameraSize;
    }
}
