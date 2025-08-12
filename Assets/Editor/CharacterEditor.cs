using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using CommonEnum;
using static Character;

[CustomEditor(typeof(Character))]
public class CharacterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Character crusader = (Character)target;

        // Draw default inspector property editor
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (Application.isPlaying && crusader._abilities != null)
        {
            EditorGUILayout.LabelField("Abilities:", EditorStyles.boldLabel);
            GUILayout.BeginVertical("box");

            foreach (KeyValuePair<string, object> ability in crusader._abilities)
            {
                EditorGUILayout.LabelField("  " + ability.Key + ": " + ability.Value.ToString());
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Resistances", EditorStyles.boldLabel);
            GUILayout.BeginVertical("box");

            foreach (KeyValuePair<string, Resistances> resistance in crusader._resistances)
            {
                EditorGUILayout.LabelField(resistance.Key, resistance.Value.ToString());
            }
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Equipment", EditorStyles.boldLabel);
            GUILayout.BeginVertical("box");

            foreach (KeyValuePair<string, EquipmentLevels> equipment in crusader._equipments)
            {
                EditorGUILayout.LabelField(equipment.Key, equipment.Value.ToString());
            }

            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.Label("½ºÅ³", EditorStyles.boldLabel);
            GUILayout.BeginVertical("box");
            // Display skills
            EditorGUILayout.LabelField("Skills:", EditorStyles.boldLabel);
            foreach (KeyValuePair<string, object> skill in crusader._skills)
            {
                EditorGUILayout.LabelField("  " + skill.Key + ":");
                foreach (KeyValuePair<string, object> detail in (Dictionary<string, object>)skill.Value)
                {
                    EditorGUILayout.LabelField("    " + detail.Key + ": " + detail.Value.ToString());
                }
            }
            GUILayout.EndVertical();
        }
    }
}



