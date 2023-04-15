
using System;
using Microsoft.MixedReality.Toolkit;
using RealityCollective.ServiceFramework.Definitions;
using RealityCollective.ServiceFramework.Interfaces;
using Unity.Barracuda;
using UnityEngine;

namespace YoloHolo.Services
{
    [CreateAssetMenu(menuName = "YoloProcessorProfile", fileName = "YoloProcessorProfile",
        order = (int)CreateProfileMenuItemIndices.ServiceConfig)]
    public class YoloProcessorProfile : BaseServiceProfile<IServiceModule>
    {
        [SerializeField]
        private NNModel model;
        public NNModel Model => model;

        [SerializeField]
        private float minimumProbability = 0.65f;
        public float MinimumProbability => minimumProbability;

        [SerializeField]
        private float overlapThreshold = 0.5f;
        public float OverlapThreshold => overlapThreshold;

        [SerializeField] 
        private int channels = 3;

        public int Channels => channels;

        [SerializeField]
        [Implements(typeof(IYoloClassTranslator), TypeGrouping.ByNamespaceFlat)]
        private SystemType classTranslator;

        private static IYoloClassTranslator translator;

        public IYoloClassTranslator ClassTranslator => 
            translator ??= (IYoloClassTranslator)Activator.CreateInstance(classTranslator);
    }
}
