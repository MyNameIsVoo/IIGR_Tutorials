using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace IIGR.Utils
{
	public static class CommonUtils
	{
		public static bool CheckPointInPerimeterArea(Vector3 point, Vector3[] points, float coefficient)
		{
			var length = points.Length;
			var inside = false;

			var p1 = points[0];
			for (var i = 1; i <= length; i++)
			{
				var p2 = points[i % length];
				if (point.z > Math.Min(p1.z, p2.z) && point.z <= Math.Max(p1.z, p2.z) && point.x <= Math.Max(p1.x, p2.x) && p1.z != p2.z)
				{
					if (p1.x == p2.x || point.x <= (point.z - p1.z) * (p2.x - p1.x) / (p2.z - p1.z) + p1.x)
						inside = !inside;
				}
				p1 = p2;
			}

			return inside;
		}

		public static double CalculateArea(Vector3[] points)
		{
			if (points.Length != 4)
				throw new ArgumentException($"We need 4 points! [{points.Length}]");

			var side1 = points[1] - points[0];
			var side2 = points[3] - points[0];

			return Mathf.Abs(side1.x * side2.z - side1.z * side2.x);
		}

		public static void CreateFolder(string fullPath)
		{
			if (!Directory.Exists(fullPath))
				Directory.CreateDirectory(fullPath);
		}

		public static void WriteFileBin<T>(T data, string path)
		{
			CreateFolder(Path.GetFullPath(path));

			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("WriteFileBin: path is null");
			if (data == null)
				throw new ArgumentNullException($"WriteFileBin: data is null | {path}");

			try
			{
				var bf = new BinaryFormatter();
				var file = File.Create(GetPath(path));
				bf.Serialize(file, data);
				file.Close();
				Debug.Log($"Saving Completed: {path}");
			}
			catch (Exception ex)
			{
				throw new Exception("Save data exception", ex);
			}
		}

		public static T ReadFileBin<T>(string path)
		{
			if (string.IsNullOrEmpty(path))
				throw new ArgumentNullException("ReadFileBin: path is null");

			var dataPath = GetPath(path);
			if (!File.Exists(dataPath))
			{
				Debug.LogError($"ReadFileBin: not found path - {path}");
				return default(T);
			}

			try
			{
				var bf = new BinaryFormatter();
				var file = File.Open(dataPath, FileMode.Open);
				var data = (T)bf.Deserialize(file);
				file.Close();
				Debug.Log($"Loading Completed: {path}");
				return data;
			}
			catch (Exception ex)
			{
				throw new Exception("Load data exception", ex);
			}
		}

		public static string GetPath(string path)
		{
			var filePath = Path.GetFullPath(".");
			filePath = filePath.Replace("\\", "/") + path;
			return filePath;
		}
	}
}