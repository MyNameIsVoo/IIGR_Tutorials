using UnityEngine;

namespace IIGR.Culling
{
	public interface IGrassCulling
	{
		public long Id { get; }
		public Vector3[] RectPoints { get; }
		public float Perimeter { get; }

		public void CalculateGrassCulling(bool forceCalculate = false);
	}
}