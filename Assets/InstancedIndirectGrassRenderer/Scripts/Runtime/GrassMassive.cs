using IIGR.Data;
using System.Collections.Generic;
using UnityEngine;

namespace IIGR
{
    [System.Serializable]
    public class GrassMassive
    {
        public GrassData Data { get; private set; } = new GrassData(new List<Vector3>(), new List<float>());
        public int VisibleAmount { get; private set; }
        public GrassCellData[] CellDatas { get; set; }

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

        private void UpdateTotalGrassAmount()
        {
            VisibleAmount = Data.Positions.Count;
            _grassRenderer.Recreate();
        }
    }
}