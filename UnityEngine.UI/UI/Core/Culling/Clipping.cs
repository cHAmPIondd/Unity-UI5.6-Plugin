using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class Clipping
    {
        /// <summary>
        /// �������Ĳü�Rect����Ч��validRect=false
        /// </summary>
        /// <param name="rectMaskParents"></param>
        /// <param name="validRect"></param>
        /// <returns></returns>
        public static Rect FindCullAndClipWorldRect(List<RectMask2D> rectMaskParents, out bool validRect)
        {
            if (rectMaskParents.Count == 0)
            {
                validRect = false;
                return new Rect();
            }

            //����ж��RectMask2D�����������ཻ��Rect
            var compoundRect = rectMaskParents[0].canvasRect;
            for (var i = 0; i < rectMaskParents.Count; ++i)//�����Զ�����һ�Σ�iӦ�ô�1��ʼ
                compoundRect = RectIntersect(compoundRect, rectMaskParents[i].canvasRect);

            var cull = compoundRect.width <= 0 || compoundRect.height <= 0;
            if (cull)
            {
                validRect = false;
                return new Rect();
            }
            //�����俴����һ���ɱƣ��б�Ҫ�𣿣�
            Vector3 point1 = new Vector3(compoundRect.x, compoundRect.y, 0.0f);
            Vector3 point2 = new Vector3(compoundRect.x + compoundRect.width, compoundRect.y + compoundRect.height, 0.0f);
            
            validRect = true;
            return new Rect(point1.x, point1.y, point2.x - point1.x, point2.y - point1.y);
        }

        /// <summary>
        /// ��������Rect�ཻ��Rect
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static Rect RectIntersect(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.x, b.x);
            float xMax = Mathf.Min(a.x + a.width, b.x + b.width);
            float yMin = Mathf.Max(a.y, b.y);
            float yMax = Mathf.Min(a.y + a.height, b.y + b.height);
            if (xMax >= xMin && yMax >= yMin)
                return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
            return new Rect(0f, 0f, 0f, 0f);
        }
    }
}
