using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IIGR.Utils
{
    public static class Extentions
    {
        public static bool IsNotNull<T>(this List<T> list)
        {
            return list != null && list.IsNotEmpty();
        }

        public static bool IsNotEmpty<T>(this List<T> list)
        {
            return list.Count != 0;
        }

		public static bool IsNotEmpty<T>(this IEnumerable<T> list)
		{
			return list.Any();
		}

		public static bool IsNotNull<T>(this T[] list)
		{
			return list != null && list.Length != 0;
		}

		public static SerializableVector3 ToSerializableVector3(this Vector3 thiz)
		{
			return new SerializableVector3(thiz);
		}
	}
}