using Unity.XR.CoreUtils;
using UnityEngine;

namespace MRTKExtensions.MagicLeap.SpatialMap
{
    public class OffsetToZero : MonoBehaviour
    {
        void Start()
        {
            if (SystemInfo.deviceModel.Contains("Magic Leap"))
            {
                transform.position = Vector3.zero;
                GetComponent<XROrigin>().CameraYOffset = 0;
            }
        }
    }
}

