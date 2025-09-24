using System;
using UnityEngine;

namespace IIGR.Utils
{
	public static class CommonUtils
	{
		public static bool CheckPointInPerimeterArea(Vector3 point, Vector3[] points, float coefficient)
		{
			var length = points.Length;
			var inside = false;

			var p1 = points[0];
			for (var i = 1; i <= length; i++)
			{
				var p2 = points[i % length];
				if (point.z > Math.Min(p1.z, p2.z) && point.z <= Math.Max(p1.z, p2.z) && point.x <= Math.Max(p1.x, p2.x) && p1.z != p2.z)
				{
					if (p1.x == p2.x || point.x <= (point.z - p1.z) * (p2.x - p1.x) / (p2.z - p1.z) + p1.x)
						inside = !inside;
				}
				p1 = p2;
			}

			return inside;
		}

		public static double CalculateArea(Vector3[] points)
		{
			if (points.Length != 4)
				throw new ArgumentException($"We need 4 points! [{points.Length}]");

			var side1 = points[1] - points[0];
			var side2 = points[3] - points[0];

			return Mathf.Abs(side1.x * side2.z - side1.z * side2.x);
		}
	}
}