using DevicePipe;
using UnityEngine;
using UnityEngine.UI;

namespace DeviceViz
{
    /// <summary>
    /// Base class for visualization layers.
    /// Each layer produces a Texture and displays it on a RawImage.
    /// </summary>
    public abstract class VizLayer : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] protected RawImage _target;

        /// <summary>Output texture for this layer.</summary>
        public abstract Texture texture { get; }

        /// <summary>Target RawImage for display.</summary>
        public RawImage target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>Show or hide this layer.</summary>
        public bool visible
        {
            get => _target != null && _target.enabled;
            set { if (_target) _target.enabled = value; }
        }

        /// <summary>Upload and encode raw pressure data to this layer.</summary>
        public abstract void UpdateData(int[] newData, int width, int height);

        /// <summary>Handle touch pressure info. Default no-op.</summary>
        public virtual void UpdateTouches(PressureInfo[] touches, int width, int height) { }

        /// <summary>Clear all rendered content on this layer.</summary>
        public abstract void Clear();
    }
}
