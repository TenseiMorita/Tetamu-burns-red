using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CorridorGenerator))]
public class CorridorGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields
        DrawDefaultInspector();

        CorridorGenerator generator = (CorridorGenerator)target;

        GUILayout.Space(15);
        
        GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f); // Emerald green for Generate button!
        if (GUILayout.Button("Generate Corridor", GUILayout.Height(35)))
        {
            // Register Undo for generation so it can be reverted easily
            Undo.RegisterCreatedObjectUndo(generator.gameObject, "Generate Procedural Corridor");
            generator.Generate();
        }

        GUILayout.Space(5);

        GUI.backgroundColor = new Color(0.8f, 0.25f, 0.2f); // Crimson red for Clear button!
        if (GUILayout.Button("Clear Corridor", GUILayout.Height(25)))
        {
            generator.Clear();
        }
        
        GUI.backgroundColor = Color.white; // Reset color
    }
}
