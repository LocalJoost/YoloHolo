using UnityEngine;
using YoloHolo.Services;

namespace YoloHolo.YoloLabeling
{
    public class YoloGameObject 
    {
        public Vector2 ImagePosition { get; }
        public string Name {get; }
        public GameObject DisplayObject { get; set; }
        public Vector3? PositionInSpace { get; set; }
        public float TimeLastSeen { get; set; }

        private const int MaxLabelDistance = 10;
        private const float SphereCastSize = 0.15f;
        private const string SpatialMeshLayerName  = "Spatial Mesh";

        public YoloGameObject(
            YoloItem yoloItem, Transform cameraTransform, 
            Vector2Int cameraSize, Vector2Int yoloImageSize,
            float virtualProjectionPlaneWidth)
        {
            ImagePosition = new Vector2(
                (yoloItem.Center.x / yoloImageSize.x * cameraSize.x - cameraSize.x / 2) / cameraSize.x,
                (yoloItem.Center.y / yoloImageSize.y * cameraSize.y - cameraSize.y / 2) / cameraSize.y);
            Name = yoloItem.MostLikelyObject;

            var virtualProjectionPlaneHeight = virtualProjectionPlaneWidth * cameraSize.y / cameraSize.x;
            FindPositionInSpace(cameraTransform, virtualProjectionPlaneWidth, virtualProjectionPlaneHeight);
            TimeLastSeen = Time.time;
        }

        private void FindPositionInSpace(Transform transform,
            float width, float height)
        {
            var positionOnPlane = transform.position + transform.forward + 
                transform.right * (ImagePosition.x * width) - 
                transform.up * (ImagePosition.y * height);
            PositionInSpace = CastOnSpatialMap(positionOnPlane, transform);
        }

        private Vector3? CastOnSpatialMap(Vector3 positionOnPlane, Transform transform)
        {
            if (Physics.SphereCast(transform.position, SphereCastSize,
                    (positionOnPlane - transform.position),
                    out var hitInfo, MaxLabelDistance, LayerMask.GetMask(SpatialMeshLayerName)))
            {
                return hitInfo.point;
            }
            return null;
        }

    }
}
