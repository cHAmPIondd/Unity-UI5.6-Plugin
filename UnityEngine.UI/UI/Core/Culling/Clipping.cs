using System.Collections.Generic;

namespace UnityEngine.UI
{
    public static class Clipping
    {
        /// <summary>
        /// 返回最后的裁剪Rect，无效则validRect=false
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

            //如果有多个RectMask2D，计算他们相交的Rect
            var compoundRect = rectMaskParents[0].canvasRect;
            for (var i = 0; i < rectMaskParents.Count; ++i)//很明显多算了一次，i应该从1开始
                compoundRect = RectIntersect(compoundRect, rectMaskParents[i].canvasRect);

            var cull = compoundRect.width <= 0 || compoundRect.height <= 0;
            if (cull)
            {
                validRect = false;
                return new Rect();
            }
            //这两句看得我一脸蒙逼，有必要吗？？
            Vector3 point1 = new Vector3(compoundRect.x, compoundRect.y, 0.0f);
            Vector3 point2 = new Vector3(compoundRect.x + compoundRect.width, compoundRect.y + compoundRect.height, 0.0f);
            
            validRect = true;
            return new Rect(point1.x, point1.y, point2.x - point1.x, point2.y - point1.y);
        }

        /// <summary>
        /// 计算两个Rect相交的Rect
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
