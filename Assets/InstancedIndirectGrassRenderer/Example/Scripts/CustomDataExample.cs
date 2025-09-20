using IIGR;
using IIGR.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace IIGR_Example
{
    [InitializeOnLoad]
    public class CustomDataExample : MonoBehaviour
    {
        public const int MaxAmount = 40000000;

		[Range(100, MaxAmount)]
        [SerializeField] private int _instanceCount = 100000;
		[SerializeField] private Vector2 _heightLimits = new Vector2(0.2f, 0.6f);
        [Range(0.1f, 3f)]
		[SerializeField] private float _areaOffset = 0.5f;

        private int _cacheCount = -1;

        private void Update()
        {
            UpdatePosIfNeeded();
        }

        private void OnDisable()
        {
            _cacheCount = -1;
        }

        private void UpdatePosIfNeeded()
        {
            if (InstancedIndirectGrassRenderer.Instance == null || _instanceCount == _cacheCount || InstancedIndirectGrassRenderer.Instance.Massive.Data == null)
                return;

            var timer = new Stopwatch();
            timer.Start();

            UnityEngine.Random.InitState(123);

            var scale = Mathf.Sqrt(_instanceCount * 0.25f) * 0.5f;
            var objectTransform = transform;
            objectTransform.localScale = new Vector3(scale, objectTransform.localScale.y, scale);

            var positions = new List<Vector3>(_instanceCount);
            var heights = new List<float>();
            for (int i = 0; i < _instanceCount; i++)
            {
                var pos = Vector3.zero;
                pos.x = UnityEngine.Random.Range(-_areaOffset, _areaOffset) * objectTransform.lossyScale.x;
                pos.z = UnityEngine.Random.Range(-_areaOffset, _areaOffset) * objectTransform.lossyScale.z;
                pos += objectTransform.position;
                positions.Add(new Vector3(pos.x, pos.y, pos.z));
                heights.Add(UnityEngine.Random.Range(_heightLimits.x, _heightLimits.y));
            }

            var grassData = new GrassData(positions, heights);
            InstancedIndirectGrassRenderer.Instance.AddRange(grassData, true);
            _cacheCount = positions.Count;

            timer.Stop();
            Debug.Log($"Generate positions = {timer.Elapsed}");
        }
    }
}