using IIGR;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace IIGR_Editor
{
	[CustomEditor(typeof(InstancedIndirectGrassRenderer))]
	public class InstancedIndirectGrassRendererEditor : Editor
	{
		private EditorCoroutine _coroutine;

		private void OnEnable()
		{
			InstancedIndirectGrassRenderer.Instance?.StopAllCoroutines();
		}

		public override void OnInspectorGUI()
		{
			if (InstancedIndirectGrassRenderer.Instance == null)
			{
				EditorGUILayout.HelpBox("The InstancedIndirectGrassRenderer is not initialized!", MessageType.Warning);
				return;
			}

			var isBusy = InstancedIndirectGrassRenderer.Instance.IsBusy;
			GUI.enabled = !isBusy;

			if (isBusy)
				EditorGUILayout.HelpBox("Busy...", MessageType.Warning);

			base.OnInspectorGUI();

			EditorGUILayout.Separator();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Bake Culling (slow)"))
				EditorApplication.delayCall = BakeCulling;
			if (GUILayout.Button("Clear"))
				EditorApplication.delayCall = InstancedIndirectGrassRenderer.Instance.RebuildGrass;
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			var hasSaveData = InstancedIndirectGrassRenderer.Instance.Massive?.Data?.IsNotNull() ?? false;
			GUI.enabled = hasSaveData && !isBusy;
			if (GUILayout.Button($"Save{(hasSaveData ? " [it can takes a lot of time]" : " [data is empty]")}"))
				EditorApplication.delayCall = InstancedIndirectGrassRenderer.Instance.SaveData;
			var isExistSaveData = InstancedIndirectGrassRenderer.Instance.IsExistSaveData;
			GUI.enabled = isExistSaveData && !isBusy;
			if (GUILayout.Button($"Load{(isExistSaveData ? " [it can takes a lot of time]" : " [save not found]")}"))
				EditorApplication.delayCall = InstancedIndirectGrassRenderer.Instance.LoadDataAsync;
			GUI.enabled = !isBusy;
			EditorGUILayout.EndHorizontal();

			GUI.enabled = true;
		}

		private void BakeCulling()
		{
			if (_coroutine != null)
				EditorCoroutineUtility.StopCoroutine(_coroutine);
			_coroutine = EditorCoroutineUtility.StartCoroutine(InstancedIndirectGrassRenderer.Instance.BakeCullingAsync(null), this);
		}
	}
}