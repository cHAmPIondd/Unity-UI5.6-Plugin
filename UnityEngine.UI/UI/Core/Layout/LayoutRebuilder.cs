using System.Collections.Generic;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    public class LayoutRebuilder : ICanvasElement
    {
        private RectTransform m_ToRebuild;
        //There are a few of reasons we need to cache the Hash fromt he transform:
        //  - This is a ValueType (struct) and .Net calculates Hash from the Value Type fields.
        //  - The key of a Dictionary should have a constant Hash value.
        //  - It's possible for the Transform to get nulled from the Native side.
        // We use this struct with the IndexedSet container, which uses a dictionary as part of it's implementation
        // So this struct gets used as a key to a dictionary, so we need to guarantee a constant Hash value.
        private int m_CachedHashFromTransform;

        static ObjectPool<LayoutRebuilder> s_Rebuilders = new ObjectPool<LayoutRebuilder>(null, x => x.Clear());

        private void Initialize(RectTransform controller)
        {
            m_ToRebuild = controller;
            m_CachedHashFromTransform = controller.GetHashCode();
        }

        private void Clear()
        {
            m_ToRebuild = null;
            m_CachedHashFromTransform = 0;
        }

        static LayoutRebuilder()
        {
            RectTransform.reapplyDrivenProperties += ReapplyDrivenProperties;
        }

        static void ReapplyDrivenProperties(RectTransform driven)
        {
            MarkLayoutForRebuild(driven);
        }

        public Transform transform { get { return m_ToRebuild; }}

        public bool IsDestroyed()
        {
            return m_ToRebuild == null;
        }

        /// <summary>
        /// 从components中移除禁用的组件
        /// </summary>
        /// <param name="components"></param>
        static void StripDisabledBehavioursFromList(List<Component> components)
        {
            components.RemoveAll(e => e is Behaviour && !((Behaviour)e).isActiveAndEnabled);
        }

        public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot)
        {
            var rebuilder = s_Rebuilders.Get();
            rebuilder.Initialize(layoutRoot);
            rebuilder.Rebuild(CanvasUpdate.Layout);
            s_Rebuilders.Release(rebuilder);
        }
        /// <summary>
        /// 重建布局
        /// </summary>
        /// <param name="executing"></param>
        public void Rebuild(CanvasUpdate executing)
        {
            switch (executing)
            {
                case CanvasUpdate.Layout:
                    // It's unfortunate that we'll perform the same GetComponents querys for the tree 2 times,
                    // but each tree have to be fully iterated before going to the next action,
                    // so reusing the results would entail storing results in a Dictionary or similar,
                    // which is probably a bigger overhead than performing GetComponents multiple times.
                    PerformLayoutCalculation(m_ToRebuild, e => (e as ILayoutElement).CalculateLayoutInputHorizontal());
                    PerformLayoutControl(m_ToRebuild, e => (e as ILayoutController).SetLayoutHorizontal());
                    PerformLayoutCalculation(m_ToRebuild, e => (e as ILayoutElement).CalculateLayoutInputVertical());
                    PerformLayoutControl(m_ToRebuild, e => (e as ILayoutController).SetLayoutVertical());
                    break;
            }
        }
        /// <summary>
        /// 自顶向下确定各个布局物体的具体位置和大小
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="action"></param>
        private void PerformLayoutControl(RectTransform rect, UnityAction<Component> action)
        {
            if (rect == null)
                return;

            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutController), components);
            StripDisabledBehavioursFromList(components);

            // If there are no controllers on this rect we can skip this entire sub-tree
            // We don't need to consider controllers on children deeper in the sub-tree either,
            // since they will be their own roots.
            if (components.Count > 0)
            {
                // Layout control needs to executed top down with parents being done before their children,
                // because the children rely on the sizes of the parents.

                // First call layout controllers that may change their own RectTransform
                for (int i = 0; i < components.Count; i++)
                    if (components[i] is ILayoutSelfController)
                        action(components[i]);

                // Then call the remaining, such as layout groups that change their children, taking their own RectTransform size into account.
                for (int i = 0; i < components.Count; i++)
                    if (!(components[i] is ILayoutSelfController))
                        action(components[i]);

                for (int i = 0; i < rect.childCount; i++)
                    PerformLayoutControl(rect.GetChild(i) as RectTransform, action);
            }

            ListPool<Component>.Release(components);
        }

        /// <summary>
        /// 自低向上计算各个布局组物体的Min,Preferred,Flexible
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="action"></param>
        private void PerformLayoutCalculation(RectTransform rect, UnityAction<Component> action)
        {
            if (rect == null)
                return;

            var components = ListPool<Component>.Get();
            rect.GetComponents(typeof(ILayoutElement), components);
            StripDisabledBehavioursFromList(components);

            // If there are no controllers on this rect we can skip this entire sub-tree
            // We don't need to consider controllers on children deeper in the sub-tree either,
            // since they will be their own roots.
            if (components.Count > 0)
            {
                // Layout calculations needs to executed bottom up with children being done before their parents,
                // because the parent calculated sizes rely on the sizes of the children.
                for (int i = 0; i < rect.childCount; i++)
                    PerformLayoutCalculation(rect.GetChild(i) as RectTransform, action);

                for (int i = 0; i < components.Count; i++)
                    action(components[i]);
            }

            ListPool<Component>.Release(components);
        }
        /// <summary>
        /// 查找最顶层的布局组，并让它重建
        /// </summary>
        /// <param name="rect"></param>
        public static void MarkLayoutForRebuild(RectTransform rect)
        {
            if (rect == null)
                return;

            //找到最顶层的布局组(LayoutGroup)
            RectTransform layoutRoot = rect;
            while (true)
            {
                var parent = layoutRoot.parent as RectTransform;
                if (!ValidLayoutGroup(parent))
                    break;
                layoutRoot = parent;
            }

            // We know the layout root is valid if it's not the same as the rect,
            // since we checked that above. But if they're the same we still need to check.
            if (layoutRoot == rect && !ValidController(layoutRoot))
                return;

            MarkLayoutRootForRebuild(layoutRoot);
        }
        /// <summary>
        /// 检查是否是合法的布局组
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private static bool ValidLayoutGroup(RectTransform parent)
        {
            if (parent == null)
                return false;
            var comps = ListPool<Component>.Get();
            parent.GetComponents(typeof(ILayoutGroup), comps);
            StripDisabledBehavioursFromList(comps);
            var validCount = comps.Count > 0;
            ListPool<Component>.Release(comps);
            return validCount;
        }
        /// <summary>
        /// 检查是否是合法的布局控制器
        /// </summary>
        /// <param name="layoutRoot"></param>
        /// <returns></returns>
        private static bool ValidController(RectTransform layoutRoot)
        {
            if (layoutRoot == null)
                return false;

            var comps = ListPool<Component>.Get();
            layoutRoot.GetComponents(typeof(ILayoutController), comps);
            StripDisabledBehavioursFromList(comps);
            var valid =  comps.Count > 0;
            ListPool<Component>.Release(comps);
            return valid;
        }

        /// <summary>
        /// 用这个类的一个对象包含着controller，并代理contoller重建
        /// </summary>
        /// <param name="controller"></param>
        private static void MarkLayoutRootForRebuild(RectTransform controller)
        {
            if (controller == null)
                return;

            var rebuilder = s_Rebuilders.Get();
            rebuilder.Initialize(controller);
            if (!CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(rebuilder))
                s_Rebuilders.Release(rebuilder);
        }

        public void LayoutComplete()
        {
            s_Rebuilders.Release(this);
        }

        public void GraphicUpdateComplete()
        {}

        public override int GetHashCode()
        {
            return m_CachedHashFromTransform;
        }

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return "(Layout Rebuilder for) " + m_ToRebuild;
        }
    }
}
