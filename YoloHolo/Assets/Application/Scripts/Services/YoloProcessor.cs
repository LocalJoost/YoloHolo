using System.Collections.Generic;
using System.Threading.Tasks;
using RealityCollective.ServiceFramework.Services;
using Unity.Sentis;
using UnityEngine;

namespace YoloHolo.Services
{
    [System.Runtime.InteropServices.Guid("c585457f-2408-4e23-a6e4-e76612e61058")]
    public class YoloProcessor : BaseServiceWithConstructor, IYoloProcessor
    {
        private readonly YoloProcessorProfile profile;
        private Worker worker;

        public YoloProcessor(string name, uint priority, YoloProcessorProfile profile)
            : base(name, priority)
        {
            this.profile = profile;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            // Load the YOLO model from the provided NNModel asset
            var model = ModelLoader.Load(profile.Model);
            worker = new Worker(model, BackendType.GPUCompute);
        }

        public async Task<List<YoloItem>> RecognizeObjects(Texture2D texture)
        {
            // Create a tensor from the input texture
            try
            {
                var inputTensor = TextureConverter.ToTensor(texture);

                await Task.Delay(32);
                // Run the model on the input tensor
                var outputTensor = await ForwardAsync(worker, inputTensor);

                var result = outputTensor.ReadbackAndClone();

                inputTensor.Dispose();
                outputTensor.Dispose();

                var yoloItems = result.GetYoloData(profile.ClassTranslator,
                    profile.MinimumProbability, profile.OverlapThreshold, profile.Version);

                result.Dispose();
                return yoloItems;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error recognizing objects: {e.Message} {e.StackTrace}");
                return new List<YoloItem>();
            }
        }

        // Nicked from https://docs.unity3d.com/Packages/com.unity.sentis@2.1/manual/split-inference-over-multiple-frames.html
        private async Task<Tensor<float>> ForwardAsync(Worker modelWorker, Tensor inputs)
        {
            var executor = worker.ScheduleIterable(inputs);
            var it = 0;
            bool hasMoreWork;
            do
            {
                hasMoreWork = executor.MoveNext();
                if (++it % 20 == 0)
                {
                    await Task.Delay(32);
                }
            } while (hasMoreWork);

            var result = modelWorker.PeekOutput() as Tensor<float>;

            return result;
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            // Dispose of the worker when it is no longer needed
            worker?.Dispose();
        }
    }
}
