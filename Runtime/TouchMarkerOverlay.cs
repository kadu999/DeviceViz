using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    public class TouchMarkerOverlay
    {
        readonly RectTransform[] _markers;
        readonly Image[] _images;
        readonly Transform _parent;
        static Sprite _circle;

        public TouchMarkerOverlay(Transform parent, int count = 10)
        {
            _parent = parent;
            _markers = new RectTransform[count];
            _images = new Image[count];
            for (int i = 0; i < count; i++)
                (_markers[i], _images[i]) = CreateMarker(i);
        }

        (RectTransform, Image) CreateMarker(int i)
        {
            var go = new GameObject($"Touch{i}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_parent, false);
            go.transform.SetAsLastSibling();
            var img = go.GetComponent<Image>();
            img.sprite = CircleSprite;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            go.SetActive(false);
            return (rt, img);
        }

        public void Update(DevicePipe.PressureInfo[] touches, int cols, int rows)
        {
            float w = ((RectTransform)_parent).rect.width;
            float scale = w / cols;

            for (int i = 0; i < _markers.Length; i++)
            {
                if (i < touches.Length)
                {
                    var t = touches[i];
                    _markers[i].anchoredPosition = new Vector2(t.y * scale, t.x * scale);
                    _markers[i].sizeDelta = Vector2.one * t.radius * 2f * scale;
                    _images[i].color = new Color(1, 0, 0, t.pressure / 100f);
                    _markers[i].gameObject.SetActive(true);
                }
                else
                    _markers[i].gameObject.SetActive(false);
            }
        }

        static Sprite CircleSprite
        {
            get
            {
                if (_circle == null)
                {
                    int s = 64;
                    var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
                    var colors = new Color[s * s];
                    float r = s / 2f;
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                            colors[y * s + x] = (x - r) * (x - r) + (y - r) * (y - r) <= r * r ? Color.white : Color.clear;
                    tex.SetPixels(colors);
                    tex.Apply();
                    _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
                }
                return _circle;
            }
        }
    }
}
