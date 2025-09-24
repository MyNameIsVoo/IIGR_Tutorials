using IIGR.Data;
using System.Collections.Generic;
using UnityEngine;

namespace IIGR
{
	public class HiddenGrassArea
	{
		public long Id { get; private set; }
		public readonly GrassCellData[] CellDatas;

		public bool IsValid => CellDatas != null && CellDatas.Length != 0;

		public HiddenGrassArea(long id, int maxCells)
		{
			Id = id;
			CellDatas = new GrassCellData[maxCells];
			for (var i = 0; i < maxCells; i++)
				CellDatas[i] = new GrassCellData()
				{
					Positions = new List<Vector3>(0),
					Heights = new List<float>(0)
				};
		}

		public void Add(int cell, Vector3 position, float height)
		{
			CellDatas[cell].Positions.Add(position);
			CellDatas[cell].Heights.Add(height);
		}
	}
}