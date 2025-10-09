using IIGR;
using IIGR.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace IIGR_Example
{
	[RequireComponent(typeof(Terrain))]
	public class TerrainDataExample : MonoBehaviour
	{
		public event Action OnBakingStart;
		public event Action OnBakingFinish;

		public Vector2 HeightLimits = new Vector2(0.2f, 0.6f);

		private Terrain _terrain;
		private readonly List<Vector3> _points = new();
		private readonly List<float> _heights = new();
		private bool _hasBeenCalculated;

		private void Awake()
		{
			_terrain = GetComponent<Terrain>();
		}

		private IEnumerator Start()
		{
			yield return null;

			Bake();
		}

#if UNITY_EDITOR
		[ContextMenu("Build Grass")]
		public void CalculateObject()
		{
			CalculatePoints();
		}
#endif

		public void Bake()
		{
			if (InstancedIndirectGrassRenderer.Instance == null)
				throw new ArgumentException("The InstancedIndirectGrassRenderer is Null!");
			if (InstancedIndirectGrassRenderer.Instance.IsBusy)
				throw new ArgumentException("Waiting the InstancedIndirectGrassRenderer complete calculating...");

			OnBakingStart?.Invoke();
			CalculatePoints();
			InstancedIndirectGrassRenderer.Instance.StartCoroutine(InstancedIndirectGrassRenderer.Instance.BakeCullingAsync(OnBakingFinish));
		}

		private void CalculatePoints()
		{
			var timer = new Stopwatch();
			timer.Start();

#if UNITY_EDITOR
			_terrain = GetComponent<Terrain>();
#endif

			if (!_hasBeenCalculated)
			{
				UnityEngine.Random.InitState(123);
				var terrainData = _terrain.terrainData;
				var resolution = terrainData.heightmapResolution;
				var width = terrainData.size.x;
				var depth = terrainData.size.z;
				var heights = terrainData.GetHeights(0, 0, resolution, resolution);
				_points.Clear();
				_heights.Clear();
				var terrainPosition = _terrain.transform.position;
				for (var x = 0; x < resolution; x++)
				{
					for (var z = 0; z < resolution; z++)
					{
						var vertexX = terrainPosition.x + x * width / (resolution - 1);
						var vertexZ = terrainPosition.z + z * depth / (resolution - 1);
						var vertexY = terrainPosition.y + heights[x, z] * terrainData.size.y;

						_points.Add(new Vector3(vertexZ, vertexY, vertexX));
						_heights.Add(UnityEngine.Random.Range(HeightLimits.x, HeightLimits.y));
					}
				}
			}

			var grassData = new GrassData(_points, _heights);
			InstancedIndirectGrassRenderer.Instance.AddRange(grassData, true);
			_hasBeenCalculated = true;

			timer.Stop();
			Debug.Log($"Build Grass = {timer.Elapsed} with {_heights.Count} instances");
		}
	}
}