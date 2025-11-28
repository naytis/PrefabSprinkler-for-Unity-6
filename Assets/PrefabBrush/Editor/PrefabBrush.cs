using UnityEngine;
using UnityEditor;

public class PrefabBrush : EditorWindow
{
    string objectName = "";
    float spawnRadius = 1.0f;
    int objectCount = 10;
    float minDistance = 0.5f; // ƒобавлено поле минимального рассто€ни€
    GameObject prefabToSpawn;

    [MenuItem("Tools/PrefabBrush")]
    public static void ShowWindow()
    {
        GetWindow(typeof(PrefabBrush));
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Brush Settings", EditorStyles.boldLabel);
        prefabToSpawn = (GameObject)EditorGUILayout.ObjectField("Prefab to Spawn", prefabToSpawn, typeof(GameObject), false);
        objectName = EditorGUILayout.TextField("Object Name", objectName);
        objectCount = EditorGUILayout.IntField("Object Count", objectCount);
        spawnRadius = EditorGUILayout.Slider("Spawn Radius", spawnRadius, 1f, 100f);
        minDistance = EditorGUILayout.Slider("Min Distance", minDistance, 0.1f, 10f); // ƒобавлено поле в меню
        if (GUILayout.Button("Spawn Prefabs"))
        {
            SpawnPrefabs();
        }
    }
    private void SpawnPrefabs()
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Please assign a prefab to spawn.");
            return;
        }
        var positions = new System.Collections.Generic.List<Vector3>();
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = objectCount * 20; // ограничение попыток
        while (spawned < objectCount && attempts < maxAttempts)
        {
            Vector3 randomPosition = Random.insideUnitSphere * spawnRadius;
            randomPosition.z = 0; // Keep on ground level
            bool tooClose = false;
            foreach (var pos in positions)
            {
                if (Vector3.Distance(pos, randomPosition) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
            {
                GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);
                newObject.transform.position = randomPosition;
                newObject.name = string.IsNullOrEmpty(objectName) ? prefabToSpawn.name : objectName;
                Undo.RegisterCreatedObjectUndo(newObject, "Spawn Prefab");
                positions.Add(randomPosition);
                spawned++;
            }
            attempts++;
        }
        if (spawned < objectCount)
        {
            Debug.LogWarning($"Could only spawn {spawned} objects with the given min distance.");
        }
    }
}
