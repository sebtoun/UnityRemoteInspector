using System.Collections.Generic;
using UnityEngine;

namespace RemoteInspector
{
    public static class UtilityExtensions
    {
        public static string FullPath( this Transform tr )
        {
            var path = tr.name;
            while ( ( tr = tr.parent ) != null )
            {
                path = tr.name + "/" + path;
            }

            return path;
        }

        public static IEnumerable<Transform> WalkUpwards( this Transform tr )
        {
            yield return tr;
            while ( ( tr = tr.parent ) != null )
            {
                yield return tr;
            }
        }
    }
}