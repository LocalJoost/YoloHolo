using System;
using Unity.Barracuda;
using UnityEngine;

namespace YoloHolo.Services
{
    public abstract class YoloItem
    {
        public Vector2 Center { get; protected set; }

        public Vector2 Size { get; protected set; }

        public Vector2 TopLeft { get; protected set; }

        public Vector2 BottomRight { get; protected set; }

        public float Confidence { get; protected set; }

        public string MostLikelyObject { get; protected set; }

        public static YoloItem Create(Tensor tensorData, int boxIndex, IYoloClassTranslator translator,
            YoloVersion version)
        {
            if (version == YoloVersion.V7)
            {
                return new YoloV7Item(tensorData, boxIndex, translator);
            }

            if (version == YoloVersion.V8)
            {
                return new YoloV8Item(tensorData, boxIndex, translator);
            }

            throw new ArgumentException($"Unsupported Yolo version {version}");
        }
    }
}