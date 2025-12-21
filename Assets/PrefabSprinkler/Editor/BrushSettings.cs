using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class BrushSettings // Класс для хранения всех настроек кисти
{
    public float spawnRadius = 1.0f; // Радиус/размер кисти
    public float objectDensity = 0.5f; // Плотность объектов (0 - редко, 1 - густо)
    public List<GameObject> prefabsToSpawn = new List<GameObject>(); // Список префабов для размещения
    public bool brushMode = false; // Режим кисти активен
    public bool eraserMode = false; // Режим ластика активен
    public float spawnInterval = 0.1f; // Интервал между спавнами при зажатой ЛКМ
    public float lineAngle = 0f; // Угол наклона линии в градусах (для линейной кисти)
    public bool randomFlipY = false; // Случайное отзеркаливание по оси Y
    public int currentBrushShape = 1; // Текущая форма кисти: 0=Single, 1=Circle, 2=Square, 3=Line
    
    // Проверяет, является ли префаб 2D-объектом (содержит SpriteRenderer)
    private bool Is2DPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        
        // Проверяем наличие SpriteRenderer в самом префабе или дочерних объектах
        SpriteRenderer spriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
        return spriteRenderer != null;
    }
    
    public List<GameObject> GetValidPrefabs() // Получает список только валидных (непустых) префабов
    {
        return prefabsToSpawn.FindAll(p => p != null && Is2DPrefab(p));
    }
    
    public bool HasValidPrefabs() // Проверяет, есть ли хотя бы один валидный префаб
    {
        return GetValidPrefabs().Count > 0;
    }
    
    // Возвращает список префабов с ошибками (не 2D)
    public List<GameObject> GetInvalid2DPrefabs()
    {
        return prefabsToSpawn.FindAll(p => p != null && !Is2DPrefab(p));
    }
}
