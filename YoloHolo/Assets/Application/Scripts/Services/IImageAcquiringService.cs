
using System.Threading.Tasks;
using RealityCollective.ServiceFramework.Interfaces;
using UnityEngine;

namespace YoloHolo.Services
{
    public interface IImageAcquiringService : IService
    {
        void Initialize(Vector2Int requestedImageSize);
        Vector2Int ActualCameraSize { get; }
        Task<Texture2D> GetImage();
    }
}