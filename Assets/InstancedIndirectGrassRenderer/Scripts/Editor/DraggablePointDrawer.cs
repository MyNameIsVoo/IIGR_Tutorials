using IIGR.Utils;
using UnityEditor;
using UnityEngine;

namespace IIGR_Editor
{
	[CustomEditor(typeof(MonoBehaviour), true)]
	public class DraggablePointDrawer : Editor
	{
		readonly GUIStyle style = new GUIStyle();

		private void OnEnable()
		{
			style.fontStyle = FontStyle.Bold;
			style.normal.textColor = Color.white;
		}

		public void OnSceneGUI()
		{
			var property = serializedObject.GetIterator();
			while (property.Next(true))
			{
				if (property.propertyType == SerializedPropertyType.Vector3)
				{
					handleVectorProperty(property);
				}
				else if (property.isArray)
				{
					for (var x = 0; x < property.arraySize; x++)
					{
						var element = property.GetArrayElementAtIndex(x);
						if (element.propertyType != SerializedPropertyType.Vector3) // Break early if we're not an array of Vector3
							break;
						handleVectorPropertyInArray(element, property, x);
					}
				}
			}
		}

		private void handleVectorProperty(SerializedProperty property)
		{
			var field = serializedObject.targetObject.GetType().GetField(property.name);
			if (field == null)
				return;

			var draggablePoints = field.GetCustomAttributes(typeof(DraggablePointAttribute), false);
			if (draggablePoints.Length > 0)
			{
				Handles.Label(property.vector3Value + ((MonoBehaviour)target).transform.position, property.name);
				property.vector3Value = Handles.PositionHandle(property.vector3Value + ((MonoBehaviour)target).transform.position, Quaternion.identity) - ((MonoBehaviour)target).transform.position;
				serializedObject.ApplyModifiedProperties();
			}
		}

		private void handleVectorPropertyInArray(SerializedProperty property, SerializedProperty parent, int index)
		{
			var parentfield = serializedObject.targetObject.GetType().GetField(parent.name);
			if (parentfield == null)
				return;

			var draggablePoints = parentfield.GetCustomAttributes(typeof(DraggablePointAttribute), false);
			if (draggablePoints.Length > 0)
			{
				Handles.Label(property.vector3Value + ((MonoBehaviour)target).transform.position, parent.name + "[" + index + "]");
				property.vector3Value = Handles.PositionHandle(property.vector3Value + ((MonoBehaviour)target).transform.position, Quaternion.identity) - ((MonoBehaviour)target).transform.position;
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}