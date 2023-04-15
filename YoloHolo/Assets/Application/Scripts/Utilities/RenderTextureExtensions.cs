using UnityEngine;

namespace YoloHolo.Utilities
{
    //https://stackoverflow.com/questions/44264468/convert-rendertexture-to-texture2d
    public static class RenderTextureExtensions
    {
        public static Texture2D ToTexture2D(this RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.RGB24, false);
            var oldRt = RenderTexture.active;
            RenderTexture.active = rTex;

            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();

            RenderTexture.active = oldRt;
            return tex;
        }
    }
}
