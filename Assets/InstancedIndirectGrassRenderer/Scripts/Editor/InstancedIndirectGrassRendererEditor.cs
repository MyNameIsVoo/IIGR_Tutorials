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