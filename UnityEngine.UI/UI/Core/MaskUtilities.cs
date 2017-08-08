using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    public class MaskUtilities
    {
        /// <summary>
        /// 通知子物体重新更新裁剪父物体
        /// </summary>
        /// <param name="mask"></param>
        public static void Notify2DMaskStateChanged(Component mask)
        {
            var components = ListPool<Component>.Get();
            mask.GetComponentsInChildren(components);
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] == null || components[i].gameObject == mask.gameObject)
                    continue;

                var toNotify = components[i] as IClippable;
                if (toNotify != null)
                    toNotify.RecalculateClipping();
            }
            ListPool<Component>.Release(components);
        }

        /// <summary>
        /// 通知子物体重新更新遮罩深度
        /// </summary>
        /// <param name="mask"></param>
        public static void NotifyStencilStateChanged(Component mask)
        {
            var components = ListPool<Component>.Get();
            mask.GetComponentsInChildren(components);
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i] == null || components[i].gameObject == mask.gameObject)
                    continue;

                var toNotify = components[i] as IMaskable;
                if (toNotify != null)
                    toNotify.RecalculateMasking();
            }
            ListPool<Component>.Release(components);
        }

        /// <summary>
        /// 向上查找Canvas并返回
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public static Transform FindRootSortOverrideCanvas(Transform start)
        {
            // find stencil value
            var element = start;
            Transform lastCanvas = null;
            while (element != null)
            {
                var canvas = element.GetComponent<Canvas>();

                if (canvas != null && canvas.overrideSorting)
                    return element;

                if (canvas != null)
                    lastCanvas = element;

                element = element.parent;
            }
            return lastCanvas;
        }

        /// <summary>
        /// 返回模板深度（即自此到Canvas有几个父物体有Mask）
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="stopAfter"></param>
        /// <returns></returns>
        public static int GetStencilDepth(Transform transform, Transform stopAfter)
        {
            var depth = 0;
            if (transform == stopAfter)
                return depth;

            var t = transform.parent;
            var components = ListPool<Component>.Get();
            while (t != null)
            {
                t.GetComponents(typeof(Mask), components);
                for (var i = 0; i < components.Count; ++i)
                {
                    if (components[i] != null && ((Mask)components[i]).IsActive() && ((Mask)components[i]).graphic.IsActive())
                    {
                        ++depth;
                        break;
                    }
                }

                if (t == stopAfter)
                    break;

                t = t.parent;
            }
            ListPool<Component>.Release(components);
            return depth;
        }
        /// <summary>
        /// 返回物体父物体的首个RectMask2D
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static RectMask2D GetRectMaskForClippable(IClippable transform)
        {
            var t = transform.rectTransform.parent;
            var components = ListPool<Component>.Get();
            while (t != null)
            {
                t.GetComponents(typeof(RectMask2D), components);
                for (var i = 0; i < components.Count; ++i)
                {
                    if (components[i] != null && ((RectMask2D)components[i]).IsActive())
                    {
                        var result = (RectMask2D)components[i];
                        ListPool<Component>.Release(components);
                        return result;
                    }
                }

                var canvas = t.GetComponent<Canvas>();
                if (canvas)
                    break;
                t = t.parent;
            }
            ListPool<Component>.Release(components);
            return null;
        }

        /// <summary>
        /// 获取物体及其所有父物体的全部RectMask2D存在masks中
        /// </summary>
        /// <param name="clipper"></param>
        /// <param name="masks"></param>
        public static void GetRectMasksForClip(RectMask2D clipper, List<RectMask2D> masks)
        {
            masks.Clear();

            var t = clipper.transform;
            var components = ListPool<Component>.Get();
            while (t != null)
            {
                t.GetComponents(typeof(RectMask2D), components);
                for (var i = 0; i < components.Count; ++i)
                {
                    if (components[i] != null && ((RectMask2D)components[i]).IsActive())
                        masks.Add((RectMask2D)components[i]);
                }

                var canvas = t.GetComponent<Canvas>();
                if (canvas)
                    break;
                t = t.parent;
            }
            ListPool<Component>.Release(components);
        }
    }
}
