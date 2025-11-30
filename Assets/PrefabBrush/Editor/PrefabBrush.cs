using UnityEngine;
using UnityEditor;

public class PrefabBrush : EditorWindow // Главный класс - окно редактора для работы с кистью префабов
{
    private BrushSettings settings = new BrushSettings(); // Настройки кисти
    private BrushVisualizer visualizer; // Визуализатор кисти на сцене
    private PrefabSpawner spawner; // Спавнер префабов
    private PrefabEraser eraser; // Ластик для удаления объектов
    private bool isMouseDown = false; // Флаг зажатия левой кнопки мыши
    private float lastSpawnTime = 0f; // Время последнего спавна
    private Vector2 scrollPosition; // Позиция скролла для списка префабов
    private GUIContent undoIcon, brushIcon, eraserIcon, cursorIcon, singleIcon, circleIcon, squareIcon, lineIcon; // Иконки UI

    [MenuItem("Tools/PrefabBrush")] // Создает пункт меню
    public static void ShowWindow() { GetWindow(typeof(PrefabBrush)); }

    private void OnEnable() // Вызывается при открытии окна
    {
        visualizer = new BrushVisualizer(settings); // Инициализируем компоненты
        spawner = new PrefabSpawner(settings);
        eraser = new PrefabEraser(settings);
        SceneView.duringSceneGui += OnSceneGUI; // Подписываемся на события Scene View
        LoadIcons();
    }

    private void OnDisable() { SceneView.duringSceneGui -= OnSceneGUI; } // Отписываемся при закрытии
    
    // Загружает иконки для кнопок
    private void LoadIcons()
    {
        undoIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_undo.png") as Texture2D, "Undo");
        brushIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_brush.png") as Texture2D, "Brush Mode");
        eraserIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_eraser.png") as Texture2D, "Eraser Mode");
        cursorIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_cursor.png") as Texture2D, "Normal Cursor");
        singleIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_single.png") as Texture2D, "Single Object");
        circleIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_circle.png") as Texture2D, "Circle Brush");
        squareIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_square.png") as Texture2D, "Square Brush");
        lineIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_line.png") as Texture2D, "Line Brush");
    }

    private void OnGUI() // Отрисовка UI окна редактора
    {
        DrawToolbar();
        EditorGUILayout.Space(10);
        GUILayout.Label(settings.eraserMode ? "Eraser Mode" : "Brush Mode", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        DrawBrushShapeButtons();
        EditorGUILayout.Space(10);
        DrawPrefabList();
        EditorGUILayout.Space(10);
        DrawBrushSettings();
        if (Event.current.type == EventType.Repaint) GUI.FocusControl(null);
        DrawInfoBox();
        HandleHotkeys();
    }
    
    // Рисует верхнюю панель инструментов с кнопками режимов
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(!Undo.GetCurrentGroupName().Contains("Spawn Prefab") && Undo.GetCurrentGroup() == 0);
        if (GUILayout.Button(undoIcon, GUILayout.Width(30), GUILayout.Height(30))) Undo.PerformUndo(); // Undo
        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = settings.brushMode ? Color.gray : Color.white;
        if (GUILayout.Button(brushIcon, GUILayout.Width(30), GUILayout.Height(30))) { settings.brushMode = !settings.brushMode; settings.eraserMode = false; SceneView.RepaintAll(); } // Кисть
        GUI.backgroundColor = settings.eraserMode ? Color.gray : Color.white;
        if (GUILayout.Button(eraserIcon, GUILayout.Width(30), GUILayout.Height(30))) { settings.eraserMode = !settings.eraserMode; settings.brushMode = false; SceneView.RepaintAll(); } // Ластик
        GUI.backgroundColor = (!settings.brushMode && !settings.eraserMode) ? Color.gray : Color.white;
        if (GUILayout.Button(cursorIcon, GUILayout.Width(30), GUILayout.Height(30))) { settings.brushMode = false; settings.eraserMode = false; isMouseDown = false; SceneView.RepaintAll(); } // Курсор
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
    
    // Рисует кнопки выбора формы кисти
    private void DrawBrushShapeButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = settings.currentBrushShape == 0 ? Color.gray : Color.white;
        if (GUILayout.Button(singleIcon, GUILayout.Width(30), GUILayout.Height(30))) settings.currentBrushShape = 0; // Single
        GUI.backgroundColor = settings.currentBrushShape == 1 ? Color.gray : Color.white;
        if (GUILayout.Button(circleIcon, GUILayout.Width(30), GUILayout.Height(30))) settings.currentBrushShape = 1; // Circle
        GUI.backgroundColor = settings.currentBrushShape == 2 ? Color.gray : Color.white;
        if (GUILayout.Button(squareIcon, GUILayout.Width(30), GUILayout.Height(30))) settings.currentBrushShape = 2; // Square
        GUI.backgroundColor = settings.currentBrushShape == 3 ? Color.gray : Color.white;
        if (GUILayout.Button(lineIcon, GUILayout.Width(30), GUILayout.Height(30))) settings.currentBrushShape = 3; // Line
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }
    
    // Рисует список префабов с возможностью добавления/удаления
    private void DrawPrefabList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Prefabs to Spawn", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(100));
        for (int i = 0; i < settings.prefabsToSpawn.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            settings.prefabsToSpawn[i] = (GameObject)EditorGUILayout.ObjectField(settings.prefabsToSpawn[i], typeof(GameObject), false);
            if (GUILayout.Button("X", GUILayout.Width(25))) { settings.prefabsToSpawn.RemoveAt(i); EditorGUILayout.EndHorizontal(); break; }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Prefab")) settings.prefabsToSpawn.Add(null);
        if (GUILayout.Button("Clear All") && settings.prefabsToSpawn.Count > 0) settings.prefabsToSpawn.Clear();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
    
    // Рисует настройки кисти (размер, плотность и т.д.)
    private void DrawBrushSettings()
    {
        GUILayout.Label(settings.eraserMode ? "Eraser Settings" : "Brush Settings", EditorStyles.boldLabel);
        if (!settings.eraserMode) settings.randomFlipY = EditorGUILayout.Toggle("Random Mirroring", settings.randomFlipY);
        if (settings.currentBrushShape == 3) // Линейная кисть
        {
            settings.spawnRadius = EditorGUILayout.Slider(settings.eraserMode ? "Eraser Length" : "Line Length", settings.spawnRadius, 0.5f, 10f);
            settings.lineAngle = EditorGUILayout.Slider("Line Angle", settings.lineAngle, 0f, 180f);
        }
        else if (settings.currentBrushShape != 0) settings.spawnRadius = EditorGUILayout.Slider(settings.eraserMode ? "Eraser Size" : "Brush Size", settings.spawnRadius, 0.5f, 10f);
        if (!settings.eraserMode && settings.currentBrushShape != 0) settings.objectDensity = EditorGUILayout.Slider("Object Density", settings.objectDensity, 0.1f, 1f);
    }
    
    // Рисует информационное сообщение о текущем режиме
    private void DrawInfoBox()
    {
        if (settings.brushMode || settings.eraserMode)
        {
            EditorGUILayout.Space(10);
            string shapeText = settings.currentBrushShape == 0 ? "Single Object" : settings.currentBrushShape == 1 ? "Circle" : settings.currentBrushShape == 2 ? "Square" : "Line";
            EditorGUILayout.HelpBox(settings.eraserMode ? $"Eraser Mode Active ({shapeText}): Click and hold on the scene to erase prefabs" : $"Brush Mode Active ({shapeText}): Click and hold on the scene to spawn prefabs", MessageType.Info);
        }
    }
    
    private void HandleHotkeys() // Обрабатывает горячие клавиши
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.Z) { Undo.PerformUndo(); e.Use(); } // Ctrl+Z
    }

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
