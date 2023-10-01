using System;
using System.Threading.Tasks;
using RealityCollective.ServiceFramework.Services;
using UnityEngine;
using UnityEngine.XR.MagicLeap;
using YoloHolo.Utilities;

namespace YoloHolo.Services
{
    [System.Runtime.InteropServices.Guid("74cb7d73-e972-4bbc-8888-0165efadd52c")]
    public class MLImageAcquiringService : BaseServiceWithConstructor, IImageAcquiringService
    {
        private readonly ImageAcquiringServiceProfile profile;
        private Vector2Int imageSize;

        public MLImageAcquiringService(string name, uint priority, ImageAcquiringServiceProfile profile)
            : base(name, priority)
        {
            this.profile = profile;
        }

        public void Initialize(Vector2Int requestedImageSize)
        {
            imageSize = requestedImageSize;
        }

        public override void Start()
        {
            StartCameraCapture();
        }

        private async Task StartCameraCapture(
            MLCameraBase.Identifier cameraIdentifier = MLCameraBase.Identifier.Main,
            MLCameraBase.CaptureFrameRate targetFrameRate = MLCameraBase.CaptureFrameRate._15FPS,
            MLCameraBase.OutputFormat outputFormat = MLCameraBase.OutputFormat.RGBA_8888)
        {
            bool isCameraAvailable = await WaitForCameraAvailabilityAsync(cameraIdentifier);

            if (isCameraAvailable)
            {
                await ConnectAndConfigureCameraAsync(cameraIdentifier, targetFrameRate, outputFormat);
            }
        }

        private async Task<bool> WaitForCameraAvailabilityAsync(MLCameraBase.Identifier cameraIdentifier)
        {
            bool cameraDeviceAvailable = false;
            int maxAttempts = 10;
            int attempts = 0;

            while (!cameraDeviceAvailable && attempts < maxAttempts)
            {
                MLResult result =
                    MLCameraBase.GetDeviceAvailabilityStatus(cameraIdentifier, out cameraDeviceAvailable);

                if (result.IsOk == false && cameraDeviceAvailable == false)
                {
                    // Wait until the camera device is available
                    await Task.Delay(TimeSpan.FromSeconds(1.0f));
                }

                attempts++;
            }

            return cameraDeviceAvailable;
        }

        private async Task<bool> ConnectAndConfigureCameraAsync(MLCameraBase.Identifier cameraIdentifier,
            MLCameraBase.CaptureFrameRate targetFrameRate, MLCameraBase.OutputFormat outputFormat)
        {
            MLCameraBase.ConnectContext context = CreateCameraContext(cameraIdentifier);
            var captureCamera = await MLCamera.CreateAndConnectAsync(context);
            if (captureCamera == null)
            {
                return false;
            }

            bool hasImageStreamCapabilities =
                GetStreamCapabilityWBestFit(captureCamera, out MLCameraBase.StreamCapability streamCapability);
            if (!hasImageStreamCapabilities)
            {
                await DisconnectCameraAsync(captureCamera);
                return false;
            }

            // Try to configure the camera based on our target configuration values
            MLCameraBase.CaptureConfig captureConfig =
                CreateCaptureConfig(streamCapability, targetFrameRate, outputFormat);
            var prepareResult = captureCamera.PrepareCapture(captureConfig, out MLCameraBase.Metadata _);
            if (!MLResult.DidNativeCallSucceed(prepareResult.Result, nameof(captureCamera.PrepareCapture)))
            {
                await DisconnectCameraAsync(captureCamera);
                return false;
            }

            bool captureStarted = await StartVideoCaptureAsync(captureCamera);
            if (!captureStarted)
            {
                await DisconnectCameraAsync(captureCamera);
                return false;
            }

            return true;
        }

        private async Task<bool> StartVideoCaptureAsync(MLCamera captureCamera)
        {
            // Trigger auto exposure and white balance
            await captureCamera.PreCaptureAEAWBAsync();

            var startCapture = await captureCamera.CaptureVideoStartAsync();
            var isCapturingVideo =
                MLResult.DidNativeCallSucceed(startCapture.Result, nameof(captureCamera.CaptureVideoStart));

            if (!isCapturingVideo)
            {
                return false;
            }

            captureCamera.OnRawVideoFrameAvailable += OnCaptureRawVideoFrameAvailable;
            return true;
        }

        private MLCameraBase.ConnectContext CreateCameraContext(MLCameraBase.Identifier cameraIdentifier)
        {
            var context = MLCameraBase.ConnectContext.Create();
            context.CamId = cameraIdentifier;
            context.Flags = MLCameraBase.ConnectFlag.CamOnly;
            return context;
        }

        private MLCameraBase.CaptureConfig CreateCaptureConfig(MLCameraBase.StreamCapability streamCapability,
            MLCameraBase.CaptureFrameRate targetFrameRate, MLCameraBase.OutputFormat outputFormat)
        {
            var captureConfig = new MLCameraBase.CaptureConfig
            {
                CaptureFrameRate = targetFrameRate,
                StreamConfigs = new MLCameraBase.CaptureStreamConfig[1]
            };
            captureConfig.StreamConfigs[0] = MLCameraBase.CaptureStreamConfig.Create(streamCapability, outputFormat);
            return captureConfig;
        }

        private async Task DisconnectCameraAsync(MLCamera captureCamera)
        {
            if (captureCamera != null)
            {
                await captureCamera.CaptureVideoStopAsync();
                captureCamera.OnRawVideoFrameAvailable -= OnCaptureRawVideoFrameAvailable;
                await captureCamera.DisconnectAsync();
            }
        }
        
        private bool GetStreamCapabilityWBestFit(MLCamera captureCamera, out MLCameraBase.StreamCapability streamCapability)
        {
            streamCapability = default;

            if (captureCamera == null)
            {
                return false;
            }

            MLCameraBase.StreamCapability[] streamCapabilities =
                MLCameraBase.GetImageStreamCapabilitiesForCamera(captureCamera, MLCameraBase.CaptureType.Video);

            if (streamCapabilities.Length <= 0) 
                return false;

            if (MLCameraBase.TryGetBestFitStreamCapabilityFromCollection(streamCapabilities, profile.RequestedCameraSize.x,
                    profile.RequestedCameraSize.y, MLCameraBase.CaptureType.Video,
                    out streamCapability))
            {
                return true;
            }

            streamCapability = streamCapabilities[0];
            return true;
        }

        private void OnCaptureRawVideoFrameAvailable(MLCameraBase.CameraOutput cameraOutput,
            MLCameraBase.ResultExtras resultExtras,
            MLCameraBase.Metadata metadata)
        {
            UpdateRGBTexture(ref videoTexture, cameraOutput.Planes[0]);
        }

        private void UpdateRGBTexture(ref Texture2D videoTextureRGB, MLCamera.PlaneInfo imagePlane)
        {

            if (videoTextureRGB != null &&
                (videoTextureRGB.width != imagePlane.Width || videoTextureRGB.height != imagePlane.Height))
            {
                UnityEngine.Object.Destroy(videoTextureRGB);
                videoTextureRGB = null;
            }

            if (videoTextureRGB == null)
            {
                videoTextureRGB = new Texture2D((int)imagePlane.Width, (int)imagePlane.Height, TextureFormat.RGBA32, false)
                    {
                        filterMode = FilterMode.Bilinear
                    };
            }

            int actualWidth = (int)(imagePlane.Width * imagePlane.PixelStride);
            
            if (imagePlane.Stride != actualWidth)
            {
                var newTextureChannel = new byte[actualWidth * imagePlane.Height];
                for (int i = 0; i < imagePlane.Height; i++)
                {
                    Buffer.BlockCopy(imagePlane.Data, (int)(i * imagePlane.Stride), newTextureChannel, i * actualWidth, actualWidth);
                }
                videoTextureRGB.LoadRawTextureData(newTextureChannel);
            }
            else
            {
                videoTextureRGB.LoadRawTextureData(imagePlane.Data);
            }
            ActualCameraSize = new Vector2Int(videoTextureRGB.width, videoTextureRGB.height);
            videoTextureRGB.Apply();
        }
        
        private Texture2D videoTexture;

        private RenderTexture renderTexture;
        public Vector2Int ActualCameraSize { get; private set; }

        public async Task<Texture2D> GetImage()
        {
            if (videoTexture == null)
            {
                return null;
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(imageSize.x, imageSize.y, 24);
            }
            Graphics.Blit(videoTexture, renderTexture);
            await Task.Delay(32);
            return FlipTextureVertically(renderTexture.ToTexture2D());
        }
        
        private static Texture2D FlipTextureVertically(Texture2D original)
        {
            var originalPixels = original.GetPixels();
            var newPixels = new Color[originalPixels.Length];

            var width = original.width;
            var rows = original.height;

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < rows; y++)
                {
                    newPixels[x + y * width] = originalPixels[x + (rows - y -1) * width];
                }
            }

            original.SetPixels(newPixels);
            original.Apply();
            return original;
        }
    }
}