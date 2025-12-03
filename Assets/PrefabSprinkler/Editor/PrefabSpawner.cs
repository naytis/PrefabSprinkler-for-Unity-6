using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabSpawner // Класс для создания (спавна) префабов на сцене
{
    private readonly BrushSettings settings; // Ссылка на настройки кисти
    
    public PrefabSpawner(BrushSettings settings) // Конструктор класса
    {
        this.settings = settings;
    }
    
    // Главный метод для создания префабов в указанной точке
    public void SpawnPrefabs(Vector3 center)
    {
        if (!settings.HasValidPrefabs()) // Проверяем наличие префабов для спавна
        {
            Debug.LogWarning("Please add at least one prefab to spawn.");
            return;
        }
        
        List<GameObject> validPrefabs = settings.GetValidPrefabs();
        
        if (settings.currentBrushShape == 0) // Одиночный объект
        {
            SpawnSingleObject(center, validPrefabs);
            return;
        }
        
        SpawnMultipleObjects(center, validPrefabs); // Множественный спавн
    }
    
    // Создает один объект в указанной позиции
    private void SpawnSingleObject(Vector3 position, List<GameObject> validPrefabs)
    {
        GameObject randomPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)]; // Выбираем случайный префаб
        GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(randomPrefab); // Создаем экземпляр префаба
        newObject.transform.position = position;
        newObject.name = randomPrefab.name;
        
        ApplyRandomFlip(newObject); // Применяем случайное отзеркаливание
        SetupSortingOrder(newObject); // Настраиваем порядок сортировки для 2D
        Undo.RegisterCreatedObjectUndo(newObject, "Spawn Prefab"); // Регистрируем для Undo
    }
    
    // Создает множество объектов в области кисти
    private void SpawnMultipleObjects(Vector3 center, List<GameObject> validPrefabs)
    {
        float area = CalculateArea(); // Вычисляем площадь области кисти
        int objectCount = Mathf.RoundToInt(area * settings.objectDensity * 10); // Количество объектов
        float minDistance = Mathf.Lerp(5f, 0.3f, settings.objectDensity); // Минимальное расстояние между объектами
        
        var existingPositions = GetExistingPrefabPositions(center, settings.spawnRadius); // Получаем существующие позиции
        var positions = new List<Vector3>(existingPositions);
        
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = objectCount * 20;
        
        while (spawned < objectCount && attempts < maxAttempts) // Пытаемся создать нужное количество объектов
        {
            Vector3 randomPosition = GetRandomPosition(center, minDistance); // Генерируем случайную позицию
            float randomizedMinDistance = minDistance * Random.Range(0.7f, 1.3f); // Случайная вариация расстояния
            
            if (!IsTooClose(randomPosition, positions, randomizedMinDistance)) // Проверяем расстояние до других объектов
            {
                GameObject randomPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
                GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(randomPrefab);
                newObject.transform.position = randomPosition;
                newObject.name = randomPrefab.name;
                
                ApplyRandomFlip(newObject);
                SetupSortingOrder(newObject);
                Undo.RegisterCreatedObjectUndo(newObject, "Spawn Prefab");
                positions.Add(randomPosition);
                spawned++;
            }
            attempts++;
        }
    }
    
    // Вычисляет площадь области кисти в зависимости от её формы
    private float CalculateArea()
    {
        switch (settings.currentBrushShape)
        {
            case 1: return Mathf.PI * settings.spawnRadius * settings.spawnRadius; // Circle (Круг)
            case 2: return (settings.spawnRadius * 2) * (settings.spawnRadius * 2); // Square (Квадрат)
            case 3: return settings.spawnRadius; // Line (Линия)
            default: return 0;
        }
    }
    
    // Генерирует случайную позицию внутри области кисти
    private Vector3 GetRandomPosition(Vector3 center, float minDistance)
    {
        Vector3 randomPosition;
        
        switch (settings.currentBrushShape)
        {
            case 1: // Circle (Круг)
                randomPosition = Random.insideUnitSphere * settings.spawnRadius;
                randomPosition.z = 0;
                break;
                
            case 2: // Square (Квадрат)
                randomPosition = new Vector3(
                    Random.Range(-settings.spawnRadius, settings.spawnRadius),
                    Random.Range(-settings.spawnRadius, settings.spawnRadius),
                    0
                );
                break;
                
            case 3: // Line (Линия)
                float angleRad = settings.lineAngle * Mathf.Deg2Rad; // Вычисляем направление линии с учетом угла
                Vector3 direction = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
                Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
                
                float alongLine = Random.Range(-settings.spawnRadius / 2, settings.spawnRadius / 2); // Вдоль линии
                float acrossLine = Random.Range(-minDistance * 0.5f, minDistance * 0.5f); // Перпендикулярно линии
                
                randomPosition = direction * alongLine + perpendicular * acrossLine;
                break;
                
            default:
                randomPosition = Vector3.zero;
                break;
        }
        
        return randomPosition + center;
    }
    
    // Проверяет, не находится ли позиция слишком близко к существующим объектам
    private bool IsTooClose(Vector3 position, List<Vector3> existingPositions, float minDistance)
    {
        foreach (var pos in existingPositions)
        {
            if (Vector3.Distance(pos, position) < minDistance)
                return true;
        }
        return false;
    }
    
    // Применяет случайное отзеркаливание объекта по оси X
    private void ApplyRandomFlip(GameObject obj)
    {
        if (settings.randomFlipY && Random.value > 0.5f)
        {
            Vector3 scale = obj.transform.localScale;
            scale.x *= -1;
            obj.transform.localScale = scale;
        }
    }
    
    // Настраивает порядок сортировки для корректного отображения в 2D виде сверху
    // Объекты ниже на карте (меньший Y) отображаются поверх объектов выше (больший Y)
    private void SetupSortingOrder(GameObject obj)
    {
        SpriteRenderer[] spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            sr.sortingOrder = Mathf.RoundToInt(-obj.transform.position.y * 100f); // Порядок на основе Y
        }
        
        if (spriteRenderers.Length == 0) // Если нет SpriteRenderer, проверяем другие рендереры
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_SortingOrder"))
                {
                    renderer.sharedMaterial.SetFloat("_SortingOrder", -obj.transform.position.y * 100f);
                }
            }
        }
    }
    
    // Получает позиции существующих префабов в области вокруг центра кисти
    private List<Vector3> GetExistingPrefabPositions(Vector3 center, float radius)
    {
        var existingPositions = new List<Vector3>();
        
        if (!settings.HasValidPrefabs())
            return existingPositions;
        
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            foreach (GameObject prefab in settings.GetValidPrefabs()) // Проверяем соответствие префабам
            {
                if (obj.name == prefab.name || obj.name.StartsWith(prefab.name))
                {
                    float distance = Vector3.Distance(obj.transform.position, center);
                    if (distance <= radius * 2f) // Проверяем радиус действия
                    {
                        existingPositions.Add(obj.transform.position);
                        break;
                    }
                }
            }
        }
        
        return existingPositions;
    }
}
