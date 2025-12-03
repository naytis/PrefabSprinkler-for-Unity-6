using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class PrefabSpawnerTests
{
    private BrushSettings settings;
    private PrefabSpawner spawner;
    private GameObject testPrefab;
    private string testPrefabPath = "Assets/TestPrefab.prefab";

    [SetUp]
    public void Setup()
    {
        settings = new BrushSettings();
        spawner = new PrefabSpawner(settings);

        // Создание временного GameObject
        GameObject tempObject = new GameObject("TestPrefab");
        tempObject.AddComponent<SpriteRenderer>();

        // Сохранение его как префаба
        testPrefab = PrefabUtility.SaveAsPrefabAsset(tempObject, testPrefabPath);

        // Удаление временного объекта со сцены
        Object.DestroyImmediate(tempObject);
    }

    [TearDown]
    public void TearDown()
    {
        // Удаление файла префаба
        if (File.Exists(testPrefabPath))
        {
            AssetDatabase.DeleteAsset(testPrefabPath);
        }

        // Удаление всех созданных объектов на сцене
        GameObject[] spawnedObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj.name.Contains("TestPrefab"))
                Object.DestroyImmediate(obj);
        }
    }

    [Test]
    public void SpawnPrefabs_DoesNotSpawnWhenNoPrefabs()
    {
        Vector3 spawnPosition = Vector3.zero;
        int initialCount = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;

        spawner.SpawnPrefabs(spawnPosition);

        int finalCount = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
        Assert.AreEqual(initialCount, finalCount, "No objects should be spawned when prefab list is empty");
    }

    [Test]
    public void SpawnPrefabs_SpawnsSingleObjectInSingleMode()
    {
        settings.currentBrushShape = 0; // Single mode
        settings.prefabsToSpawn.Add(testPrefab);
        Vector3 spawnPosition = new Vector3(1, 1, 0);

        int initialCount = CountTestPrefabs();

        spawner.SpawnPrefabs(spawnPosition);

        int finalCount = CountTestPrefabs();
        Assert.AreEqual(initialCount + 1, finalCount, "Exactly one object should be spawned in single mode");
    }

    [Test]
    public void SpawnPrefabs_SpawnsMultipleObjectsInCircleMode()
    {
        settings.currentBrushShape = 1; // Circle mode
        settings.spawnRadius = 2.0f;
        settings.objectDensity = 0.5f;
        settings.prefabsToSpawn.Add(testPrefab);
        Vector3 spawnPosition = Vector3.zero;

        int initialCount = CountTestPrefabs();

        spawner.SpawnPrefabs(spawnPosition);

        int finalCount = CountTestPrefabs();
        Assert.Greater(finalCount, initialCount, "At least one object should be spawned in circle mode");
    }

    [Test]
    public void Constructor_InitializesWithSettings()
    {
        PrefabSpawner newSpawner = new PrefabSpawner(settings);

        Assert.IsNotNull(newSpawner, "PrefabSpawner should be initialized");
    }

    [Test]
    public void SpawnPrefabs_WithMultiplePrefabs_SpawnsRandomly()
    {
        GameObject secondPrefab = PrefabUtility.SaveAsPrefabAsset(
            new GameObject("TestPrefab2"),
            "Assets/TestPrefab2.prefab"
        );

        settings.currentBrushShape = 0; // Single mode
        settings.prefabsToSpawn.Add(testPrefab);
        settings.prefabsToSpawn.Add(secondPrefab);

        spawner.SpawnPrefabs(Vector3.zero);

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        bool foundTestPrefab = false;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("TestPrefab"))
            {
                foundTestPrefab = true;
                break;
            }
        }

        Assert.IsTrue(foundTestPrefab, "Should spawn one of the prefabs from the list");

        AssetDatabase.DeleteAsset("Assets/TestPrefab2.prefab");
    }

    // Вспомогательный метод для подсчета префабов
    private int CountTestPrefabs()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int count = 0;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("TestPrefab"))
                count++;
        }
        return count;
    }
}