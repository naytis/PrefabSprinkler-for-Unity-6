using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabEraser // Класс для удаления (стирания) объектов со сцены
{
    private readonly BrushSettings settings; // Ссылка на настройки кисти
    
    public PrefabEraser(BrushSettings settings) // Конструктор класса
    {
        this.settings = settings;
    }
    
    // Главный метод для удаления объектов в указанной области
    public void EraseObjects(Vector3 center)
    {
        if (!settings.HasValidPrefabs()) // Проверяем наличие префабов для удаления
        {
            Debug.LogWarning("Please add at least one prefab to erase.");
            return;
        }
        
        List<GameObject> objectsToDelete = FindObjectsInArea(center); // Находим объекты в области
        
        if (settings.currentBrushShape == 0 && objectsToDelete.Count > 0) // Одиночный режим
        {
            GameObject closest = FindClosestObject(objectsToDelete, center);
            Undo.DestroyObjectImmediate(closest);
        }
        else // Множественный режим - удаляем все объекты в области
        {
            foreach (GameObject obj in objectsToDelete)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }
    
    // Находит все объекты в области действия ластика
    private List<GameObject> FindObjectsInArea(Vector3 center)
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> objectsToDelete = new List<GameObject>();
        List<GameObject> validPrefabs = settings.GetValidPrefabs();
        
        foreach (GameObject obj in allObjects)
        {
            if (!MatchesPrefab(obj, validPrefabs)) // Проверяем соответствие префабам
                continue;
            
            if (IsInArea(obj.transform.position, center)) // Проверяем нахождение в области
            {
                objectsToDelete.Add(obj);
            }
        }
        
        return objectsToDelete;
    }
    
    // Проверяет, соответствует ли объект одному из префабов в списке
    private bool MatchesPrefab(GameObject obj, List<GameObject> validPrefabs)
    {
        foreach (GameObject prefab in validPrefabs)
        {
            if (obj.name == prefab.name || obj.name.StartsWith(prefab.name))
                return true;
        }
        return false;
    }
    
    // Проверяет, находится ли позиция в области действия ластика
    private bool IsInArea(Vector3 position, Vector3 center)
    {
        switch (settings.currentBrushShape)
        {
            case 0: // Single (Одиночный объект)
                return Vector3.Distance(position, center) < 0.5f;
                
            case 1: // Circle (Круг)
                return Vector3.Distance(position, center) <= settings.spawnRadius;
                
            case 2: // Square (Квадрат)
                Vector3 localPos = position - center;
                return Mathf.Abs(localPos.x) <= settings.spawnRadius && 
                       Mathf.Abs(localPos.y) <= settings.spawnRadius;
                
            case 3: // Line (Линия)
                return IsInLineArea(position, center);
                
            default:
                return false;
        }
    }
    
    // Проверяет, находится ли позиция в области линейного ластика
    private bool IsInLineArea(Vector3 position, Vector3 center)
    {
        float angleRad = settings.lineAngle * Mathf.Deg2Rad; // Направление линии с учетом угла
        Vector3 direction = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
        
        Vector3 lineStart = center - direction * (settings.spawnRadius / 2);
        Vector3 toPoint = position - lineStart;
        float projection = Vector3.Dot(toPoint, direction); // Проекция точки на направление линии
        
        if (projection >= 0 && projection <= settings.spawnRadius) // В пределах длины линии
        {
            float perpendicularDistance = Mathf.Abs(Vector3.Dot(toPoint, perpendicular));
            return perpendicularDistance <= 0.3f; // Ширина линии ластика
        }
        
        return false;
    }
    
    // Находит ближайший объект к указанному центру
    private GameObject FindClosestObject(List<GameObject> objects, Vector3 center)
    {
        GameObject closest = objects[0];
        float minDist = Vector3.Distance(closest.transform.position, center);
        
        foreach (GameObject obj in objects) // Ищем объект с минимальным расстоянием
        {
            float dist = Vector3.Distance(obj.transform.position, center);
            if (dist < minDist)
            {
                minDist = dist;
                closest = obj;
            }
        }
        
        return closest;
    }
}
