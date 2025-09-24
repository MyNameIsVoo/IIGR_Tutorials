using IIGR.Culling;
using IIGR.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace IIGR
{
    [InitializeOnLoad, DisallowMultipleComponent]
    public class InstancedIndirectGrassRenderer : MonoBehaviour
    {
		[Space(20)]
		[Header("Settings")]

        [SerializeField, Min(1)] private float _drawDistance = 300;
		[SerializeField] private UnityEngine.Rendering.ShadowCastingMode _shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
		[SerializeField] private bool _receiveShadows = true;
        [SerializeField] private Material _instanceMaterial;

		[Header("Internal")]
		[SerializeField] private ComputeShader _cullingComputeShader;

        [field: Header("Bounds")]
        [field: SerializeField] public bool CanUpdateGrass { get; private set; } = true;
        [SerializeField] private float _boundSize = 1;
		[SerializeField] private float _cellSize = 10f;
		[SerializeField] private bool _shouldBatchDispatch = true;
		[SerializeField] private LayerMask _grassLayer;

        public GrassMassive Massive { get; private set; }

        private int _cellCountX = -1;
        private int _cellCountZ = -1;
        private int _instanceCountCache = -1;
        private Mesh _cachedGrassMesh;
        private ComputeBuffer _allInstancesPosWSBuffer;
        private ComputeBuffer _allInstancesHeightBuffer;
        private ComputeBuffer _visibleInstancesOnlyPosWSIDBuffer;
        private ComputeBuffer _argsBuffer;
        private GrassCellData[] _cellDatas;
        private float _minX, _minZ, _maxX, _maxZ;
        private List<int> _visibleCellIDList = new List<int>();
        private Plane[] _cameraFrustumPlanes = new Plane[6];
        private Camera _mainCamera;
        private int _layerGrassIndex;

        public bool IsActiveBuildCulling { get; private set; }

        public static InstancedIndirectGrassRenderer Instance { get; private set; }

        private void Awake()
        {
            RebuildGrass();
        }

        private void LateUpdate()
        {
            if (!CanUpdateGrass || Massive.VisibleAmount == 0)
                return;

            UpdateAllInstanceTransformBufferIfNeeded();
            _visibleCellIDList.Clear();
            var cameraOriginalFarPlane = _mainCamera.farClipPlane;
            _mainCamera.farClipPlane = _drawDistance;
            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraFrustumPlanes);
            _mainCamera.farClipPlane = cameraOriginalFarPlane;

            for (var i = 0; i < _cellDatas.Length; i++)
            {
                var centerPosWS = new Vector3(i % _cellCountX + 0.5f, 0, i / _cellCountX + 0.5f);
                centerPosWS.x = Mathf.Lerp(_minX, _maxX, centerPosWS.x / _cellCountX);
                centerPosWS.z = Mathf.Lerp(_minZ, _maxZ, centerPosWS.z / _cellCountZ);
                var sizeValue = Mathf.Abs(_maxX - _minX) / _cellCountX + _boundSize;
                var sizeWS = new Vector3(sizeValue, 0, sizeValue);
                var cellBound = new Bounds(centerPosWS, sizeWS);

                if (GeometryUtility.TestPlanesAABB(_cameraFrustumPlanes, cellBound))
                    _visibleCellIDList.Add(i);
            }

            var v = _mainCamera.worldToCameraMatrix;
            var p = _mainCamera.projectionMatrix;
            var vp = p * v;

            _visibleInstancesOnlyPosWSIDBuffer.SetCounterValue(0);

            _cullingComputeShader.SetMatrix("_VPMatrix", vp);
            _cullingComputeShader.SetFloat("_MaxDrawDistance", _drawDistance);

            for (var i = 0; i < _visibleCellIDList.Count; i++)
            {
                var targetCellFlattenID = _visibleCellIDList[i];
                var memoryOffset = 0;
                for (var j = 0; j < targetCellFlattenID; j++)
                    memoryOffset += _cellDatas[j].Positions.Count;
                _cullingComputeShader.SetInt("_StartOffset", memoryOffset);
                var jobLength = _cellDatas[targetCellFlattenID].Positions.Count;

                if (_shouldBatchDispatch)
                {
                    while ((i < _visibleCellIDList.Count - 1) && (_visibleCellIDList[i + 1] == _visibleCellIDList[i] + 1))
                    {
                        jobLength += _cellDatas[_visibleCellIDList[i + 1]].Positions.Count;
                        i++;
                    }
                }

                var threadGroup = Mathf.Clamp(Mathf.CeilToInt(jobLength / 64f), 1, 65535);
                _cullingComputeShader.Dispatch(0, threadGroup, 1, 1);
            }

            ComputeBuffer.CopyCount(_visibleInstancesOnlyPosWSIDBuffer, _argsBuffer, 4);

            var renderBound = new Bounds();
            renderBound.SetMinMax(new Vector3(_minX, 0, _minZ), new Vector3(_maxX, 0, _maxZ));
            Graphics.DrawMeshInstancedIndirect(GetGrassMeshCache(), 0, _instanceMaterial, renderBound, _argsBuffer, 0, null, _shadowCastingMode, _receiveShadows, _layerGrassIndex
#if !UNITY_EDITOR
                , _mainCamera
#endif
                );
        }

        private void OnDisable()
        {
            Recreate();
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
#if UNITY_EDITOR
            if (IsActiveBuildCulling && !Application.isPlaying)
                EditorUtility.ClearProgressBar();
#endif
            IsActiveBuildCulling = false;
            RebuildGrass();
        }
#endif

        public void AddRange(GrassData grassData, bool clearData = false)
        {
            Massive.AddRange(grassData, clearData);
        }

        public void Recreate()
        {
            if (_argsBuffer != null)
            {
                _argsBuffer.Release();
                _argsBuffer = null;
            }

            if (_allInstancesPosWSBuffer != null)
            {
                _allInstancesPosWSBuffer.Release();
                _allInstancesPosWSBuffer = null;
            }

            if (_visibleInstancesOnlyPosWSIDBuffer != null)
            {
                _visibleInstancesOnlyPosWSIDBuffer.Release();
                _visibleInstancesOnlyPosWSIDBuffer = null;
            }

            if (_allInstancesHeightBuffer != null)
            {
                _allInstancesHeightBuffer.Release();
                _allInstancesHeightBuffer = null;
            }
        }

		internal IEnumerator BakeCullingAsync(Action callback)
		{
			yield return new WaitForSeconds(1f);

			if (Massive.VisibleAmount == 0)
			{
				Debug.LogWarning("No grass data found!");
				yield break;
			}
			if (IsActiveBuildCulling)
			{
				Debug.Log("Waiting the InstancedIndirectGrassRenderer complete calculating...");
				yield break;
			}

			var timer = new Stopwatch();
			timer.Start();

			IsActiveBuildCulling = true;
			var grassCullingObjects = FindObjectsOfType<MonoBehaviour>()
			.Where(x => x is IGrassCulling && x.gameObject.activeInHierarchy)
			.Cast<IGrassCulling>()
			.ToList();

			Debug.Log($"Bake Culling. Found: {grassCullingObjects?.Count ?? 0} objects");

			if (grassCullingObjects == null || grassCullingObjects.Count == 0)
			{
				Debug.LogWarning("Not found grass culling objects in the current scene!");
#if UNITY_EDITOR
				if (!Application.isPlaying)
					EditorUtility.ClearProgressBar();
#endif
				IsActiveBuildCulling = false;
				callback?.Invoke();
				yield break;
			}

#if UNITY_EDITOR
			if (!Application.isPlaying)
				EditorUtility.DisplayProgressBar("Bake grass culling", $"Calculating {grassCullingObjects.Count} objects", 0);
#endif
			foreach (var cullingObject in grassCullingObjects)
				cullingObject.CalculateGrassCulling(true);
			yield return Massive.HideGrassInAreaAsync(grassCullingObjects);
			yield return null;

			IsActiveBuildCulling = false;
#if UNITY_EDITOR
			if (!Application.isPlaying)
				EditorUtility.ClearProgressBar();
#endif
			timer.Stop();
			Debug.Log($"Bake Culling Async Completed: {timer.Elapsed}");
			callback?.Invoke();
		}

		internal void RebuildGrass()
        {
            Instance = this;
            Init();
        }

        private void UpdateAllInstanceTransformBufferIfNeeded()
        {
            var rootTransform = transform;
            _instanceMaterial.SetVector("_PivotPosWS", rootTransform.position);
			_instanceMaterial.SetVector("_BoundSize", new Vector2(rootTransform.localScale.x, rootTransform.localScale.z));

            if (_instanceCountCache == Massive.VisibleAmount
                && _argsBuffer != null
                && _allInstancesPosWSBuffer != null
                && _visibleInstancesOnlyPosWSIDBuffer != null
                && _allInstancesHeightBuffer != null

#if UNITY_EDITOR
                || Massive.VisibleAmount == 0
#endif
                )
                return;

            if (_allInstancesPosWSBuffer != null)
                _allInstancesPosWSBuffer.Release();
            _allInstancesPosWSBuffer = new ComputeBuffer(Massive.VisibleAmount, sizeof(float) * 3);

            if (_visibleInstancesOnlyPosWSIDBuffer != null)
                _visibleInstancesOnlyPosWSIDBuffer.Release();
            _visibleInstancesOnlyPosWSIDBuffer = new ComputeBuffer(Massive.VisibleAmount, sizeof(uint), ComputeBufferType.Append);

            if (_allInstancesHeightBuffer != null)
                _allInstancesHeightBuffer.Release();
            _allInstancesHeightBuffer = new ComputeBuffer(Massive.VisibleAmount, sizeof(float));

            _minX = float.MaxValue;
            _minZ = float.MaxValue;
            _maxX = float.MinValue;
            _maxZ = float.MinValue;
            for (var i = 0; i < Massive.VisibleAmount; i++)
            {
                var target = Massive.Data.Positions[i];
                _minX = Mathf.Min(target.x, _minX);
                _minZ = Mathf.Min(target.z, _minZ);
                _maxX = Mathf.Max(target.x, _maxX);
                _maxZ = Mathf.Max(target.z, _maxZ);
            }

            _cellCountX = Mathf.CeilToInt((_maxX - _minX) / _cellSize);
            _cellCountZ = Mathf.CeilToInt((_maxZ - _minZ) / _cellSize);

            _cellDatas = new GrassCellData[_cellCountX * _cellCountZ];
            for (var i = 0; i < _cellDatas.Length; i++)
                _cellDatas[i] = new GrassCellData()
                {
                    Positions = new List<Vector3>(),
                    Heights = new List<float>()
                };

            var minValue = _cellCountX - 1;
            for (var i = 0; i < Massive.VisibleAmount; i++)
            {
                var pos = Massive.Data.Positions[i];
                var xID = Mathf.Min(minValue, Mathf.FloorToInt(Mathf.InverseLerp(_minX, _maxX, pos.x) * _cellCountX)); 
                var zID = Mathf.Min(minValue, Mathf.FloorToInt(Mathf.InverseLerp(_minZ, _maxZ, pos.z) * _cellCountZ));

                _cellDatas[xID + zID * _cellCountX].Positions.Add(pos);
                _cellDatas[xID + zID * _cellCountX].Heights.Add(Massive.Data.Heights[i]);
            }

            var offset = 0;
            var allGrassPosWSSortedByCell = new Vector3[Massive.VisibleAmount];
            var allGrassHeightWSSortedByCell = new float[Massive.VisibleAmount];
            for (var i = 0; i < _cellDatas.Length; i++)
            {
                for (var j = 0; j < _cellDatas[i].Positions.Count; j++)
                {
                    allGrassPosWSSortedByCell[offset] = _cellDatas[i].Positions[j];
                    allGrassHeightWSSortedByCell[offset] = _cellDatas[i].Heights[j];
                    offset++;
                }
                _cellDatas[i].Init();
            }
            Massive.CellDatas = _cellDatas;

            _allInstancesPosWSBuffer.SetData(allGrassPosWSSortedByCell);
            _instanceMaterial.SetBuffer("_AllInstancesTransformBuffer", _allInstancesPosWSBuffer);
			_instanceMaterial.SetBuffer("_VisibleInstanceOnlyTransformIDBuffer", _visibleInstancesOnlyPosWSIDBuffer);
            _allInstancesHeightBuffer.SetData(allGrassHeightWSSortedByCell);
			_instanceMaterial.SetBuffer("_AllInstancesHeightBuffer", _allInstancesHeightBuffer);

            if (_argsBuffer != null)
                _argsBuffer.Release();
            var args = new uint[5] { 0, 0, 0, 0, 0 };
            _argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

            args[0] = (uint)GetGrassMeshCache().GetIndexCount(0);
            args[1] = (uint)Massive.VisibleAmount;
            args[2] = (uint)GetGrassMeshCache().GetIndexStart(0);
            args[3] = (uint)GetGrassMeshCache().GetBaseVertex(0);
            args[4] = 0;

            _argsBuffer.SetData(args);
            _instanceCountCache = Massive.VisibleAmount;

            _cullingComputeShader.SetBuffer(0, "_AllInstancesPosWSBuffer", _allInstancesPosWSBuffer);
            _cullingComputeShader.SetBuffer(0, "_AllInstancesHeightBuffer", _allInstancesHeightBuffer);
            _cullingComputeShader.SetBuffer(0, "_VisibleInstancesOnlyPosWSIDBuffer", _visibleInstancesOnlyPosWSIDBuffer);
        }

        private Mesh GetGrassMeshCache()
        {
            if (!_cachedGrassMesh)
            {
                _cachedGrassMesh = new Mesh();

                var verts = new Vector3[3];
                verts[0] = new Vector3(-0.25f, 0);
                verts[1] = new Vector3(+0.25f, 0);
                verts[2] = new Vector3(-0.0f, 1);
                var trinagles = new int[3] { 2, 1, 0, };

                _cachedGrassMesh.SetVertices(verts);
                _cachedGrassMesh.SetTriangles(trinagles, 0);
            }

            return _cachedGrassMesh;
        }

        private void Init()
        {
            Massive?.Clear();
            Massive = new GrassMassive(this);
            Massive.CellDatas = _cellDatas;
            _mainCamera = Camera.main;
            _layerGrassIndex = (int)Mathf.Log(_grassLayer.value, 2);
        }
    }
}