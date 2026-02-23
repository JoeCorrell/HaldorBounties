using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace HaldorBounties
{
    public static class TextureLoader
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly Assembly ModAssembly = Assembly.GetExecutingAssembly();
        private static readonly MethodInfo LoadImageMethod = ResolveLoadImage();

        private static MethodInfo ResolveLoadImage()
        {
            var type = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
            return type?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
        }

        public static Texture2D LoadUITexture(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            string cacheKey = "UI_" + name;
            if (Cache.TryGetValue(cacheKey, out Texture2D cached))
                return cached;

            if (LoadImageMethod == null)
            {
                HaldorBounties.Log.LogWarning("TextureLoader: ImageConversion.LoadImage not found via reflection.");
                return null;
            }

            string resourceName = $"HaldorBounties.Resources.Textures.UI.{name}.png";
            using (Stream stream = ModAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    HaldorBounties.Log.LogWarning($"TextureLoader: UI resource '{resourceName}' not found.");
                    return null;
                }

                // M-2: Stream.Read may return fewer bytes than requested in a single call
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                byte[] data = ms.ToArray();

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                bool loaded = (bool)LoadImageMethod.Invoke(null, new object[] { tex, data });
                if (loaded)
                {
                    tex.name = name;
                    Cache[cacheKey] = tex;
                    return tex;
                }

                UnityEngine.Object.Destroy(tex);
                return null;
            }
        }
    }
}
