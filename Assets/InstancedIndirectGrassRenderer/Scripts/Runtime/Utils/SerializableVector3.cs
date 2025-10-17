using System;
using UnityEngine;

namespace IIGR.Utils
{
	[Serializable]
	public class SerializableVector3
	{
		public float x;
		public float y;
		public float z;

		public SerializableVector3() { }

		public SerializableVector3(Vector3 vector)
		{
			x = vector.x;
			y = vector.y;
			z = vector.z;
		}

		public Vector3 ToVector3()
		{
			return new Vector3(x, y, z);
		}

		public static implicit operator Vector3(SerializableVector3 sv)
		{
			return sv.ToVector3();
		}

		public static implicit operator SerializableVector3(Vector3 v)
		{
			return new SerializableVector3(v);
		}
	}
}