using IIGR.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IIGR.Data
{
	public struct GrassCellData
	{
		public List<Vector3> Positions;
		public List<float> Heights;

		private Vector3[] QuadPoints;
		public Vector3Int Center;
		public bool IsValid => Positions.IsNotNull() && Heights.IsNotNull();

		public void Init()
		{
			if (!Positions.IsNotNull())
				return;

			var minX = Positions.Min(p => p.x);
			var maxX = Positions.Max(p => p.x);
			var minZ = Positions.Min(p => p.z);
			var maxZ = Positions.Max(p => p.z);

			var bottomLeft = new Vector3(minX, 0, minZ);
			var topLeft = new Vector3(minX, 0, maxZ);
			var bottomRight = new Vector3(maxX, 0, minZ);
			var topRight = new Vector3(maxX, 0, maxZ);

			QuadPoints = new Vector3[]
			{
				bottomLeft,
				topLeft,
				bottomRight,
				topRight
			};

			Center = GetRectangleCenter(QuadPoints);
		}

		public bool IsPointInsideCell(Vector3[] points) => IsValid && CheckIntersection(QuadPoints, points);

		private bool CheckIntersection(Vector3[] poly1, Vector3[] poly2)
		{
			// SAT - Separating Axis Theorem
			foreach (Vector3[] polygon in new[] { poly1, poly2 })
			{
				for (var i = 0; i < polygon.Length; i++)
				{
					var start = polygon[i];
					var end = polygon[(i + 1) % polygon.Length];

					var edge = end - start;
					var normal = Vector3.Cross(edge, Vector3.up);

					normal.Normalize();

					var min1 = float.MaxValue;
					var max1 = float.MinValue;

					foreach (Vector3 point in poly1)
					{
						var projection = Vector3.Dot(point, normal);
						min1 = Mathf.Min(min1, projection);
						max1 = Mathf.Max(max1, projection);
					}

					var min2 = float.MaxValue;
					var max2 = float.MinValue;

					foreach (Vector3 point in poly2)
					{
						var projection = Vector3.Dot(point, normal);
						min2 = Mathf.Min(min2, projection);
						max2 = Mathf.Max(max2, projection);
					}

					if (max1 < min2 || max2 < min1)
						return false;
				}
			}

			return true;
		}

		private Vector3Int GetRectangleCenter(Vector3[] points)
		{
			if (points == null || points.Length != 4)
			{
				Debug.LogError("We need 4 point to calculate rect center!");
				return Vector3Int.zero;
			}

			var sumX = 0f;
			var sumZ = 0f;

			foreach (var point in points)
			{
				sumX += point.x;
				sumZ += point.z;
			}

			var centerX = (int)(sumX * 0.25f);
			var centerZ = (int)(sumZ * 0.25f);
			var centerY = (int)points[0].y;

			return new Vector3Int(centerX, centerY, centerZ);
		}
	}
}