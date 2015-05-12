using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(ProceduralTerrainTester))]
public class ProceduralTerrainTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ProceduralTerrainTester script = (ProceduralTerrainTester)target;
        if (GUILayout.Button("Create Terrain"))
            script.CreateTerrain();
    }
}
