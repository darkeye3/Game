using System.Collections.Generic;
using Darkest.Domain.Core;
using UnityEngine;

namespace Darkest.Unity.Infrastructure
{
    /// <summary>초상화/아이콘 공급 Adapter. 실제 아트가 들어오면 이 인터페이스의 새 구현(Addressables 등)으로 교체한다.</summary>
    public interface ISpriteProvider
    {
        Sprite Get(string key);
    }

    /// <summary>
    /// Resources/Darkest/Placeholders/{key}.png 를 읽고, 없으면 key 해시 색상의 단색 스프라이트를 만든다.
    /// 텍스처 임포트 설정(Sprite/Default)에 상관없이 동작하도록 Texture2D로 읽어 Sprite.Create 한다.
    /// </summary>
    public sealed class PlaceholderSpriteProvider : ISpriteProvider
    {
        private const string ResourceFolder = "Darkest/Placeholders/";
        private const int FallbackSize = 64;
        private const float PixelsPerUnit = 100f;

        private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();
        private readonly IGameLogger _logger;

        public PlaceholderSpriteProvider(IGameLogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "unknown";
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var texture = Resources.Load<Texture2D>(ResourceFolder + key);
            if (texture == null)
            {
                _logger.Warn("Sprite", "플레이스홀더 없음 → 단색 생성: " + key);
                texture = CreateSolid(ColorFromKey(key));
            }

            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0f), PixelsPerUnit);
            sprite.name = key;
            _cache.Add(key, sprite);
            return sprite;
        }

        private static Texture2D CreateSolid(Color color)
        {
            var texture = new Texture2D(FallbackSize, FallbackSize, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color32[FallbackSize * FallbackSize];
            Color32 c = color;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Color ColorFromKey(string key)
        {
            int hash = key.GetHashCode();
            float hue = (hash & 0xFFFF) / 65535f;
            return Color.HSVToRGB(hue, 0.5f, 0.7f);
        }
    }
}
