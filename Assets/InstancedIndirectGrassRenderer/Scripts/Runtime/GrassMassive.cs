using IIGR.Culling;
using IIGR.Data;
using IIGR.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IIGR
{
	[System.Serializable]
	public class GrassMassive
	{
		public GrassData Data { get; private set; } = new GrassData(new List<Vector3>(), new List<float>());
		public int VisibleAmount { get; private set; }
		public GrassCellData[] CellDatas { get; set; }

		private bool _isActiveHiddenProcess;
		private List<HiddenGrassArea> _hiddenGrassAreas = new();
		private InstancedIndirectGrassRenderer _grassRenderer;

		public GrassMassive(InstancedIndirectGrassRenderer grassRenderer)
		{
			_grassRenderer = grassRenderer;
		}

		public void AddRange(GrassData grassData, bool clearData = false)
		{
			if (!grassData.IsNotNull())
				return;

			if (clearData)
				Data.Clear();
			Data.Add(grassData.Positions, grassData.Heights);
			UpdateTotalGrassAmount();
		}

		public void Clear()
		{
			Data.Clear();
			CellDatas = null;
			UpdateTotalGrassAmount();
		}

		public IEnumerator HideGrassInAreaAsync(List<IGrassCulling> grassCullings)
		{
			yield return new WaitForSeconds(1f);

			if (_isActiveHiddenProcess)
				yield return new WaitUntil(() => !_isActiveHiddenProcess);

			_isActiveHiddenProcess = true;
			ThreadedDataRequester.RequestData(() => CalculateGrassInArea(new CalculationData()
			{
				CellDatas = CellDatas
			}, grassCullings), HideGrassInArea);
			yield return new WaitUntil(() => !_isActiveHiddenProcess);
		}

		public void UnhideGrassInArea(int id)
		{
			if (!_grassRenderer.CanUpdateGrass || !_hiddenGrassAreas.IsNotNull())
				return;

			foreach (var hiddenGrassArea in _hiddenGrassAreas)
			{
				if (hiddenGrassArea != null && hiddenGrassArea.Id == id)
				{
					UnhideGrassInArea(hiddenGrassArea);
					_hiddenGrassAreas.Remove(hiddenGrassArea);
					return;
				}
			}
		}

		private void UnhideGrassInArea(HiddenGrassArea hiddenGrassArea)
		{
			if (!hiddenGrassArea.IsValid)
				return;

			Data.Positions.AddRange(hiddenGrassArea.CellDatas.SelectMany(x => x.Positions));
			Data.Heights.AddRange(hiddenGrassArea.CellDatas.SelectMany(x => x.Heights));

			UpdateTotalGrassAmount();
		}

		private void UpdateTotalGrassAmount()
		{
			VisibleAmount = Data.Positions.Count;
			_grassRenderer.Recreate();
		}

		private List<HiddenGrassArea> CalculateGrassInArea(CalculationData data, List<IGrassCulling> grassCullings)
		{
			if (!data.IsValid || !grassCullings.IsNotNull())
				return null;

			var hideGrassAreas = new List<HiddenGrassArea>(grassCullings.Count);
			foreach (var grassCulling in grassCullings)
			{
				var hiddenGrassArea = new HiddenGrassArea(grassCulling.Id, data.CellDatas.Length);
				for (var i = 0; i < data.CellDatas.Length; i++)
				{
					if (!data.CellDatas[i].IsPointInsideCell(grassCulling.RectPoints))
						continue;

					for (var j = 0; j < data.CellDatas[i].Positions.Count; j++)
					{
						if (CommonUtils.CheckPointInPerimeterArea(new Vector3(data.CellDatas[i].Positions[j].x, 0, data.CellDatas[i].Positions[j].z), grassCulling.RectPoints, grassCulling.Perimeter))
							hiddenGrassArea.Add(i, data.CellDatas[i].Positions[j], data.CellDatas[i].Heights[j]);
					}
				}
				hideGrassAreas.Add(hiddenGrassArea);
			}

			return hideGrassAreas;
		}

		private void HideGrassInArea(object hiddenGrassAreaObject)
		{
			if (hiddenGrassAreaObject != null
				&& hiddenGrassAreaObject is List<HiddenGrassArea> hiddenGrassAreas
				&& hiddenGrassAreas.IsNotNull()
				&& hiddenGrassAreas.Any(x => x.IsValid))
			{
				_hiddenGrassAreas.AddRange(hiddenGrassAreas);
				ThreadedDataRequester.RequestData(() => GetNewVisibleGrass(new CalculationData()
				{
					CellDatas = CellDatas
				}, hiddenGrassAreas), RecreateGrass);
				return;
			}

			_isActiveHiddenProcess = false;
		}

		private HiddenGrassArea GetNewVisibleGrass(CalculationData data, List<HiddenGrassArea> hiddenGrassAreas)
		{
			if (!data.IsValid || !hiddenGrassAreas.IsNotNull() || !hiddenGrassAreas.All(x => x.IsValid))
				return null;

			var hiddenGrassArea = new HiddenGrassArea(0, data.CellDatas.Length);
			foreach (var area in hiddenGrassAreas)
			{
				if (!area.IsValid)
					continue;

				for (var i = 0; i < area.CellDatas.Length; i++)
				{
					if (!area.CellDatas[i].IsValid)
						continue;

					hiddenGrassArea.CellDatas[i].Positions.AddRange(area.CellDatas[i].Positions);
					hiddenGrassArea.CellDatas[i].Heights.AddRange(area.CellDatas[i].Heights);
				}
			}

			var visibleGrass = new HiddenGrassArea(0, data.CellDatas.Length);
			for (var i = 0; i < data.CellDatas.Length; i++)
			{
				if (!hiddenGrassArea.CellDatas[i].IsValid)
				{
					visibleGrass.CellDatas[i].Positions = data.CellDatas[i].Positions;
					visibleGrass.CellDatas[i].Heights = data.CellDatas[i].Heights;
					continue;
				}

				visibleGrass.CellDatas[i].Positions.AddRange(data.CellDatas[i].Positions);
				visibleGrass.CellDatas[i].Heights.AddRange(data.CellDatas[i].Heights);

				for (var j = 0; j < hiddenGrassArea.CellDatas[i].Positions.Count; j++)
				{
					visibleGrass.CellDatas[i].Positions.Remove(hiddenGrassArea.CellDatas[i].Positions[j]);
					visibleGrass.CellDatas[i].Heights.Remove(hiddenGrassArea.CellDatas[i].Heights[j]);
				}
			}

			return visibleGrass;
		}

		private void RecreateGrass(object visibleGrassObject)
		{
			if (visibleGrassObject != null && visibleGrassObject is HiddenGrassArea newGrass)
			{
				Data.Clear();
				if (newGrass.IsValid)
					Data.Add(newGrass.CellDatas.SelectMany(x => x.Positions).ToList(), newGrass.CellDatas.SelectMany(x => x.Heights).ToList());
				UpdateTotalGrassAmount();
			}

			_isActiveHiddenProcess = false;
		}

		private struct CalculationData
		{
			public GrassCellData[] CellDatas;

			public bool IsValid => CellDatas != null && CellDatas.Length != 0;
		}
	}
}