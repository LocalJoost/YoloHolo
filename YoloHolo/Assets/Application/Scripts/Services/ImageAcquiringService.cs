using System.Threading.Tasks;
using RealityCollective.ServiceFramework.Services;
using UnityEngine;
using YoloHolo.Utilities;

namespace YoloHolo.Services
{
    [System.Runtime.InteropServices.Guid("74cb7d73-e972-4bbc-8a54-0165efadd52c")]
    public class ImageAcquiringService : BaseServiceWithConstructor, IImageAcquiringService
    {
        private readonly ImageAcquiringServiceProfile profile;
        private WebCamTexture webCamTexture;
        private RenderTexture renderTexture;
        
        public ImageAcquiringService(string name, uint priority, ImageAcquiringServiceProfile profile)
            : base(name, priority)
        {
            this.profile = profile;
        }
		
        public void Initialize(Vector2Int requestedImageSize)
        {
            renderTexture = new RenderTexture(requestedImageSize.x, requestedImageSize.y, 24);
            webCamTexture.Play();
        }

        public override void Start()
        {
            webCamTexture = new WebCamTexture(profile.RequestedCameraSize.x, profile.RequestedCameraSize.y, profile.CameraFPS);
            ActualCameraSize = new Vector2Int(webCamTexture.width, webCamTexture.height);
        }

        public Vector2Int ActualCameraSize { get; private set; }

        public async Task<Texture2D> GetImage()
        {
            if (renderTexture == null)
            {
                return null;
            }
            Graphics.Blit(webCamTexture, renderTexture);
            await Task.Delay(32);

            var texture = renderTexture.ToTexture2D();
            return texture;
        }
    }
}
