using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using UnityEngine;

namespace YoloHolo.Services
{
    public class YoloV8Item : YoloItem
    {
        internal YoloV8Item(Tensor tensorData, int boxIndex, IYoloClassTranslator translator)
        {
            Center = new Vector2(tensorData[0, 0, boxIndex, 0], tensorData[0, 0, boxIndex, 1]);
            Size = new Vector2(tensorData[0, 0, boxIndex, 2], tensorData[0, 0, boxIndex, 3]);
            TopLeft = Center - Size / 2;
            BottomRight = Center + Size / 2;
            Confidence = tensorData[0, 0, boxIndex, 4];

            var classProbabilities = new List<float>();
            for (var i = 5; i < tensorData.channels; i++)
            {
                classProbabilities.Add(tensorData[0, 0, boxIndex,i]);
            }
            var maxIndex = classProbabilities.Any() ? classProbabilities.IndexOf(classProbabilities.Max()) : 0;
            MostLikelyObject = translator.GetName(maxIndex);
        }
    }
}
