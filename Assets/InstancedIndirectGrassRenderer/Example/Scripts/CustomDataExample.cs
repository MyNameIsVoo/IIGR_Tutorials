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

#if UNITY_EDITOR
		[SerializeField] private bool _redrawOnChange;
		[Space(20)]
#endif

		[Range(100, MaxAmount)]
        [SerializeField] private int _instanceCount = 100000;
		[SerializeField] private Vector2 _heightLimits = new Vector2(0.2f, 0.6f);
        [Range(0.1f, 3f)]
		[SerializeField] private float _areaOffset = 0.5f;

		public Action OnBakingStart;
		public Action OnBakingFinish;

		private int _cacheCount = -1;

#if UNITY_EDITOR
		private void OnEnable()
		{
			_sliderValue = _instanceCount;
		}
#endif

		private void Update()
        {
            UpdatePosIfNeeded();
        }

        private void OnDisable()
        {
            _cacheCount = -1;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (!_redrawOnChange || !enabled
				|| InstancedIndirectGrassRenderer.Instance == null || InstancedIndirectGrassRenderer.Instance.IsBusy)
				return;

			_cacheCount = -1;
			_sliderValue = _instanceCount;
			UpdatePosIfNeeded();
		}

		private Rect windowRect = new Rect(20, 400, 450, 100);
		private int _sliderValue;

		private void OnGUI()
		{
			if (InstancedIndirectGrassRenderer.Instance == null)
				return;

			windowRect = GUI.Window(0, windowRect, DrawStatisticsWindow, "Custom Data");

			void DrawStatisticsWindow(int windowID)
			{
				GUILayout.BeginVertical();

				var fontStyle = new GUIStyle()
				{
					fontSize = 20,
					normal = { textColor = Color.white }
				};
				GUILayout.Label($"Instance Count: {_sliderValue.ToString("N0")}", fontStyle);
				_sliderValue = Mathf.Max(1, (int)GUILayout.HorizontalSlider(_sliderValue, 1, MaxAmount));

				GUI.enabled = _instanceCount != _sliderValue;

				if (GUILayout.Button("Apply"))
					_instanceCount = _sliderValue;
				if (GUILayout.Button("Bake"))
					Bake();

				GUI.enabled = true;
				GUILayout.EndVertical();

				GUI.DragWindow();
			}
		}
#endif

		[ContextMenu("Bake")]
		public void Bake()
		{
			if (InstancedIndirectGrassRenderer.Instance == null)
			{
				Debug.LogError("The InstancedIndirectGrassRenderer is Null!");
				return;
			}
			if (InstancedIndirectGrassRenderer.Instance.IsBusy)
			{
				Debug.Log("Waiting the InstancedIndirectGrassRenderer complete calculating...");
				return;
			}

			OnBakingStart?.Invoke();
			UpdatePosIfNeeded();
			InstancedIndirectGrassRenderer.Instance.StartCoroutine(InstancedIndirectGrassRenderer.Instance.BakeCullingAsync(OnBakingFinish));
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