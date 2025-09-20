using System.Collections.Generic;

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
    }
}