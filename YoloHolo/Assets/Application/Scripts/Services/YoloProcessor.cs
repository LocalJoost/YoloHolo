using System.Collections.Generic;
using System.Threading.Tasks;
using RealityCollective.ServiceFramework.Services;
using Unity.Barracuda;
using UnityEngine;

namespace YoloHolo.Services
{
    [System.Runtime.InteropServices.Guid("c585457f-2408-4e23-a6e4-e76612e61058")]
    public class YoloProcessor : BaseServiceWithConstructor, IYoloProcessor
    {
        private readonly YoloProcessorProfile profile;
        private IWorker worker;

        public YoloProcessor(string name, uint priority, YoloProcessorProfile profile)
            : base(name, priority)
        {
            this.profile = profile;
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            // Load the YOLOv7 model from the provided NNModel asset
            var model = ModelLoader.Load(profile.Model);

            // Create a Barracuda worker to run the model on the GPU
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        }

        public async Task<List<YoloItem>> RecognizeObjects(Texture2D texture)
        {
            var inputTensor = new Tensor(texture, channels: profile.Channels);
            await Task.Delay(32);
            // Run the model on the input tensor
            var outputTensor = await ForwardAsync(worker, inputTensor);
            inputTensor.Dispose();

            var yoloItems = outputTensor.GetYoloData(profile.ClassTranslator, 
                profile.MinimumProbability, profile.OverlapThreshold);

            outputTensor.Dispose();
            return yoloItems;
        }

        // Nicked from https://github.com/Unity-Technologies/barracuda-release/issues/236#issue-1049168663
        public async Task<Tensor> ForwardAsync(IWorker modelWorker, Tensor inputs)
        {
            var executor = worker.StartManualSchedule(inputs);
            var it = 0;
            bool hasMoreWork;
            do
            {
                hasMoreWork = executor.MoveNext();
                if (++it % 20 == 0)
                {
                    worker.FlushSchedule();
                    await Task.Delay(32);
                }
            } while (hasMoreWork);

            return modelWorker.PeekOutput();
        }

        /// <inheritdoc />
        public override void Destroy()
        {
            // Dispose of the Barracuda worker when it is no longer needed
            worker?.Dispose();
        }
    }
}
