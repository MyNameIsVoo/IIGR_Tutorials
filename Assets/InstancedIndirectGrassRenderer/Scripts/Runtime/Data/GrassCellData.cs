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

        private Vector3[] _quadPoints;

        public Vector3Int Center { get; private set; }

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

            _quadPoints = new Vector3[]
            {
                bottomLeft,
                topLeft,
                bottomRight,
                topRight
            };

            Center = GetRectangleCenter(_quadPoints);
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