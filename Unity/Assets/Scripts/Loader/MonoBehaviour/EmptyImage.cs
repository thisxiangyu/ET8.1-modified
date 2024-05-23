using UnityEngine;
using UnityEngine.UI;

namespace ET.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class EmptyImage : Graphic
    {
        public bool m_visible = false;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            if (m_visible)
                base.OnPopulateMesh(vh);
            else
                vh.Clear();
        }
    }
}
