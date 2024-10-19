using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;
using UnityEngine;

namespace YoloHolo.Services
{
    public class YoloV8Item : YoloItem
    {
        internal YoloV8Item(Tensor<float> tensorData, int boxIndex, IYoloClassTranslator translator)
        {
            Center = new Vector2(tensorData[0, 0, boxIndex], tensorData[0, 1, boxIndex]);
            Size = new Vector2(tensorData[0, 2, boxIndex], tensorData[0, 3, boxIndex]);
            TopLeft = Center - Size / 2;
            BottomRight = Center + Size / 2;
            Confidence = tensorData[0, 4, boxIndex];

            var classProbabilities = new List<float>();
            for (var i = 5; i < tensorData.shape[1]; i++)
            {
                classProbabilities.Add(tensorData[0, i, boxIndex]);
            }
            var maxIndex = classProbabilities.Any() ? classProbabilities.IndexOf(classProbabilities.Max()) : 0;
            MostLikelyObject = translator.GetName(maxIndex);
        }
    }
}
