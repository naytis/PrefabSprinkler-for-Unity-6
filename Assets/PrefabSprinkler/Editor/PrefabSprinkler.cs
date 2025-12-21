using UnityEngine;
using UnityEditor;

public class PrefabSprinkler : EditorWindow // Главный класс - окно редактора для работы с кистью префабов
{
    private BrushSettings settings = new BrushSettings(); // Настройки кисти
    private BrushVisualizer visualizer; // Визуализатор кисти на сцене
    private PrefabSpawner spawner; // Спавнер префабов
    private PrefabEraser eraser; // Ластик для удаления объектов
    private SprinklerUI ui; // UI компонент
    private bool isMouseDown = false; // Флаг зажатия левой кнопки мыши
    private float lastSpawnTime = 0f; // Время последнего спавна

    [MenuItem("Tools/PrefabSprinkler")] // Создает пункт меню
    public static void ShowWindow() 
    { 
        var window = GetWindow(typeof(PrefabSprinkler));
        window.titleContent = new GUIContent("PrefabSprinkler");
    }

    private void OnEnable() // Вызывается при открытии окна
    {
        visualizer = new BrushVisualizer(settings); // Инициализируем компоненты
        spawner = new PrefabSpawner(settings);
        eraser = new PrefabEraser(settings);
        ui = new SprinklerUI(settings);
        SceneView.duringSceneGui += OnSceneGUI; // Подписываемся на события Scene View
    }

    private void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; } // Отписываемся при закрытии

    private void OnGUI() { ui.DrawUI(); } // Отрисовка UI окна редактора

    private void OnSceneGUI(SceneView sceneView) // Обрабатывает события в Scene View
    {
        if (!settings.brushMode && !settings.eraserMode) return;
        Event e = Event.current;
        if (e.type == EventType.Repaint) { Vector3? point = GetScenePoint(e.mousePosition); if (point.HasValue) visualizer.DrawBrushPreview(point.Value); } // Визуализация
        if (e.type == EventType.MouseDown && e.button == 0) { isMouseDown = true; lastSpawnTime = 0f; Vector3? spawnCenter = GetScenePoint(e.mousePosition); if (spawnCenter.HasValue) ProcessBrushAction(spawnCenter.Value); e.Use(); } // ЛКМ вниз
        if (e.type == EventType.MouseUp && e.button == 0) { isMouseDown = false; e.Use(); } // ЛКМ вверх
        if (isMouseDown && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove)) // Перемещение с зажатой ЛКМ
        {
            float currentTime = (float)EditorApplication.timeSinceStartup;
            if (currentTime - lastSpawnTime >= settings.spawnInterval) { Vector3? spawnCenter = GetScenePoint(e.mousePosition); if (spawnCenter.HasValue) ProcessBrushAction(spawnCenter.Value); lastSpawnTime = currentTime; }
            e.Use();
        }
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        SceneView.RepaintAll();
    }
    
    // Получает точку на сцене из позиции мыши
    private Vector3? GetScenePoint(Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit)) return hit.point; // Попадание в коллайдер
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (plane.Raycast(ray, out float distance)) return ray.GetPoint(distance); // Плоскость z=0
        return null;
    }
    
    private void ProcessBrushAction(Vector3 center) // Обрабатывает действие кисти (спавн или удаление)
    {
        if (settings.eraserMode) eraser.EraseObjects(center);
        else spawner.SpawnPrefabs(center);
    }
}
