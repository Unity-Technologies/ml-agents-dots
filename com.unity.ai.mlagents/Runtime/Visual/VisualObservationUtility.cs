using Unity.Collections;
using UnityEngine;

namespace Unity.AI.MLAgents
{
    public static class VisualObservationUtility
    {
        /// <summary>
        /// Generates a NativeArray of floats corresponding to the camera's visual input.
        /// The Array will be of total size ( height x width x 3 )
        /// Each pixel will correspond to three consecutive floats in the order [red, green, blue]
        /// </summary>
        /// <param name="camera"> The camera used to collect the visual data</param>
        /// <param name="width"> The width of the generated image </param>
        /// <param name="height"> The height of the generated image </param>
        /// <param name="allocator"> the Allocator for the Native array </param>
        /// <returns> A native array of floats containing the image data from the camera </returns>
        public static NativeArray<float> GetVisObs(Camera camera, int width, int height, Allocator allocator = Allocator.Temp)
        {
            if (camera != null)
            {
                var texture = ObservationToTexture(camera, width, height);
                return TextureToNativeArray(texture, allocator);
            }
            return new NativeArray<float>(0, allocator, NativeArrayOptions.ClearMemory);
        }

        private static NativeArray<float> TextureToNativeArray(Texture2D texture, Allocator allocator)
        {
            var width = texture.width;
            var height = texture.height;
            var arr = new NativeArray<float>(width * height * 3, allocator, NativeArrayOptions.UninitializedMemory);

            var texturePixels = texture.GetPixels32();
            for (var h = height - 1; h >= 0; h--)
            {
                for (var w = 0; w < width; w++)
                {
                    var currentPixel = texturePixels[(height - h - 1) * width + w];
                    // For Color32, the r, g and b values are between 0 and 255.
                    arr[h * width * 3 + w * 3 + 0] = currentPixel.r / 255.0f;
                    arr[h * width * 3 + w * 3 + 1] = currentPixel.g / 255.0f;
                    arr[h * width * 3 + w * 3 + 2] = currentPixel.b / 255.0f;
                }
            }
            return arr;
        }

        private static Texture2D ObservationToTexture(Camera obsCamera, int width, int height)
        {
            var texture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
            var oldRec = obsCamera.rect;
            obsCamera.rect = new Rect(0f, 0f, 1f, 1f);
            var depth = 24;
            var format = RenderTextureFormat.Default;
            var readWrite = RenderTextureReadWrite.Default;

            var tempRt =
                RenderTexture.GetTemporary(width, height, depth, format, readWrite);

            var prevActiveRt = RenderTexture.active;
            var prevCameraRt = obsCamera.targetTexture;

            // render to offscreen texture (readonly from CPU side)
            RenderTexture.active = tempRt;
            obsCamera.targetTexture = tempRt;

            obsCamera.Render();

            texture2D.ReadPixels(new Rect(0, 0, texture2D.width, texture2D.height), 0, 0);

            obsCamera.targetTexture = prevCameraRt;
            obsCamera.rect = oldRec;
            RenderTexture.active = prevActiveRt;
            RenderTexture.ReleaseTemporary(tempRt);
            return texture2D;
        }
    }
}
