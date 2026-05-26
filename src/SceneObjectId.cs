using System.Collections.Generic;
using UnityEngine;

namespace CyberHookAP
{
    internal static class SceneObjectId
    {
        public static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            List<string> segments = new List<string>();
            Transform cursor = transform;
            while (cursor != null)
            {
                segments.Add(cursor.name + "[" + cursor.GetSiblingIndex().ToString() + "]");
                cursor = cursor.parent;
            }

            segments.Reverse();
            return string.Join("/", segments.ToArray());
        }
    }
}
