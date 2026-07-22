using DevicePipe;
using UnityEngine;

namespace DeviceViz
{
    /// <summary>
    /// Base class for visualization layers.
    /// Subclasses manage their own display (RawImage / GameObjects).
    /// </summary>
    public abstract class VizLayer : MonoBehaviour
    {
        /// <summary>Whether this layer consumes touch pressure info.</summary>
        public virtual bool needsTouches => false;

        public abstract void UpdateData(int[] newData, int width, int height);
        public virtual void UpdateTouches(PressureInfo[] touches, int width, int height) { }
        public virtual void Render() { }
        public abstract void Clear();
    }
}
