using IIGR.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace IIGR.Data
{
    [System.Serializable]
    public class GrassData
    {
        public List<Vector3> Positions = new();
        public List<float> Heights = new();

        public void Add(List<Vector3> positions, List<float> heights)
        {
            if (positions.IsNotNull())
                Positions.AddRange(positions);

            if (heights.IsNotNull())
                Heights.AddRange(heights);
        }

        public bool IsNotNull() => Positions.IsNotNull() && Heights.IsNotNull();

        public GrassData(IEnumerable<Vector3> positions, IEnumerable<float> heights)
        {
            Positions = positions != null ? new(positions) : new List<Vector3>();
			Heights = heights != null ? new(heights) : new List<float>();
		}

		public void Clear()
        {
            Positions.Clear();
            Heights.Clear();
        }
    }
}