using IIGR.Culling;
using IIGR.Utils;
using System.Linq;
using UnityEngine;

namespace IIGR
{
	public class GrassCullingObject : MonoBehaviour, IGrassCulling
	{
		[SerializeField] private long _id = 0;
		[SerializeField] private Vector3[] _points = new Vector3[4];

		private Vector3[] _rectPoints;
		private float _perimeter;
		private bool _hasCalculated;

		public long Id => _id;
		public Vector3[] RectPoints => _rectPoints;
		public float Perimeter => _perimeter;

		public void CalculateGrassCulling(bool forceCalculate = false)
		{
			if (_hasCalculated && !forceCalculate)
				return;
			_rectPoints = _points.Select(x => new Vector3(transform.position.x, 0f, transform.position.z) + x).ToArray();
			_perimeter = (float)CommonUtils.CalculateArea(_rectPoints);
			_hasCalculated = true;
		}
	}
}