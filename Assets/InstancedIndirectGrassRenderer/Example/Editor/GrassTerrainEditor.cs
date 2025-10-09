using IIGR;
using IIGR_Example;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace IIGR_Editor
{
	[CustomEditor(typeof(TerrainDataExample))]
	public class GrassTerrainEditor : Editor
	{
		private EditorCoroutine _coroutine;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.BeginHorizontal();

			GUI.enabled = !InstancedIndirectGrassRenderer.Instance.IsActiveBuildCulling;

			if (GUILayout.Button("Preview"))
				EditorApplication.delayCall = ((TerrainDataExample)target).CalculateObject;

			if (GUILayout.Button("Preview with bake"))
				EditorApplication.delayCall = PreviewWithBake;

			GUI.enabled = true;

			EditorGUILayout.EndHorizontal();
		}

		private void PreviewWithBake()
		{
			if (_coroutine != null)
				EditorCoroutineUtility.StopCoroutine(_coroutine);
			((TerrainDataExample)target).CalculateObject();
			_coroutine = EditorCoroutineUtility.StartCoroutine(InstancedIndirectGrassRenderer.Instance.BakeCullingAsync(null), this);
		}
	}
}