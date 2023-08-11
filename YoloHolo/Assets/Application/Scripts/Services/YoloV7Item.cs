using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using UnityEngine;

namespace YoloHolo.Services
{
    public class YoloV7Item : YoloItem
    {
        internal YoloV7Item(Tensor tensorData, int boxIndex, IYoloClassTranslator translator)
        {
            Center = new Vector2(tensorData[0, 0, 0, boxIndex], tensorData[0, 0, 1, boxIndex]);
            Size = new Vector2(tensorData[0, 0, 2, boxIndex], tensorData[0, 0, 3, boxIndex]);
            TopLeft = Center - Size / 2;
            BottomRight = Center + Size / 2;
            Confidence = tensorData[0, 0, 4, boxIndex];

            var classProbabilities = new List<float>();
            for (var i = 5; i < tensorData.width; i++)
            {
                classProbabilities.Add(tensorData[0, 0, i, boxIndex]);
            }
            var maxIndex = classProbabilities.Any() ? classProbabilities.IndexOf(classProbabilities.Max()) : 0;
            MostLikelyObject = translator.GetName(maxIndex);
        }
    }
}
