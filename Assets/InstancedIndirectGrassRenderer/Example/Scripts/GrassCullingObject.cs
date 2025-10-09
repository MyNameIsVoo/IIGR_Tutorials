using IIGR.Culling;
using IIGR.Utils;
using System.Linq;
using UnityEngine;

namespace IIGR
{
	public class GrassCullingObject : MonoBehaviour, IGrassCulling
	{
		[SerializeField] private long _id = 0;
		[SerializeField, DraggablePoint] private Vector3[] _points = new Vector3[4];

		private Vector3[] _rectPoints;
		private float _perimeter;
		private bool _hasCalculated;

		public long Id => _id;
		public Vector3[] RectPoints => _rectPoints;
		public float Perimeter => _perimeter;

#if UNITY_EDITOR
		private Vector3 _prevPosition;

		private void OnDrawGizmos()
		{
			if (!_points.IsNotNull())
				return;

			if (_prevPosition != transform.position)
			{
				_prevPosition = transform.position;
				CalculateGrassCulling(true);
			}

			DrawRect(_points, Color.blue);
		}

		private void DrawRect(Vector3[] rect, Color color)
		{
			Gizmos.color = color;

			for (var i = 0; i < rect.Length; i++)
			{
				Gizmos.DrawSphere(transform.position + rect[i], 0.25f);
				var nextIndex = (i + 1) % rect.Length;
				Gizmos.DrawLine(transform.position + rect[i], transform.position + rect[nextIndex]);
			}
		}

		[ContextMenu("Calculate Object")]
		private void CalculateObject()
		{
			CalculateGrassCulling(true);
		}
#endif

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