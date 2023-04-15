
using System.Collections.Generic;
using System.Threading.Tasks;
using RealityCollective.ServiceFramework.Interfaces;
using UnityEngine;

namespace YoloHolo.Services
{
    public interface IYoloProcessor : IService
    {
        Task<List<YoloItem>> RecognizeObjects(Texture2D texture);
    }
}