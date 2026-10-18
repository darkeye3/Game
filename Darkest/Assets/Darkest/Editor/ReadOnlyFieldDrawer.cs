using Darkest.Unity.Debugging;
using UnityEditor;
using UnityEngine;

namespace Darkest.Editor
{
    [CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
    public sealed class ReadOnlyFieldDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, true);
    }
}
