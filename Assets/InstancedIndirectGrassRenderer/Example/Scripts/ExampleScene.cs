using IIGR;
using UnityEngine;

namespace IIGR_Example
{
    public class ExampleScene : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvas;

        private void Awake()
        {
			_canvas.interactable = true;
			InstancedIndirectGrassRenderer.Instance.RebuildGrass();
            StartCoroutine(InstancedIndirectGrassRenderer.Instance.BakeCullingAsync(null));
		}
    }
}
