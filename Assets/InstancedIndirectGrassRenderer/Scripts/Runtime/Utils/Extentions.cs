using System.Collections.Generic;
using System.Linq;

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
	}
}