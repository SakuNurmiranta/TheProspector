using System.Collections.Generic;
using UnityEngine;

namespace SEMM91.Core.Aspects
{
    public class AspectTreeProfile
    {
        private readonly GlobalAspectRegistry _globalRegistry;

        public AspectTreeProfile(GlobalAspectRegistry globalRegistry)
        {
            this._globalRegistry = globalRegistry;
        }
        
        public List<Aspect> GetChildrenOf(string parentAspectId)
        {
            var children = new List<Aspect>();
            foreach (Aspect aspect in _globalRegistry.AspectsById.Values)
            {
                if (aspect.ParentAspectId == parentAspectId)
                {
                    children.Add(aspect);
                }
                
            }
            return children;
        }

        public List<Aspect> GetRootAspects()
        {
            var roots = new List<Aspect>();
            foreach (Aspect aspect in _globalRegistry.AspectsById.Values)
            {
                if (!aspect.HasParent)
                {
                    roots.Add(aspect);
                }
            }
            return roots;
        }

        public void DebugPrintTree()
        {
            foreach (Aspect root in GetRootAspects())
            {
                Debug.Log($"Aspect Tree Root: {root}");
                DebugPrintChildrenRecursive(root.AspectId, 1);
            }
        }

        private void DebugPrintChildrenRecursive(string parentAspectId, int depth)
        {
            foreach (Aspect child in GetChildrenOf(parentAspectId))
            {
                Debug.Log($"{new string('-', depth)}{child}");
                DebugPrintChildrenRecursive(child.AspectId, depth + 1);
            }
        }
    }
}