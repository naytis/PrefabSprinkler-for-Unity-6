using UnityEngine;
using UnityEditor;

public class SprinklerUI // Класс для отрисовки UI окна PrefabSprinkler
{
    private BrushSettings settings;
    private Vector2 scrollPosition;
    private GUIContent undoIcon, brushIcon, eraserIcon, cursorIcon, singleIcon, circleIcon, squareIcon, lineIcon;

    public SprinklerUI(BrushSettings settings)
    {
        this.settings = settings;
        LoadIcons();
    }

    // Загружает иконки для кнопок
    private void LoadIcons()
    {
        undoIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabSprinkler/Editor/icon_undo.png") as Texture2D, "Undo");
        brushIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabSprinkler/Editor/icon_brush.png") as Texture2D, "Brush Mode");
        eraserIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabSprinkler/Editor/icon_eraser.png") as Texture2D, "Eraser Mode");
        cursorIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabSprinkler/Editor/icon_cursor.png") as Texture2D, "Normal Cursor");
        singleIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabSprinkler/Editor/icon_single.png") as Texture2D, "Single Object");
        circleIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabSprinkler/Editor/icon_circle.png") as Texture2D, "Circle Brush");
        squareIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabSprinkler/Editor/icon_square.png") as Texture2D, "Square Brush");
        lineIcon = new GUIContent(EditorGUIUtility.Load("Assets/PrefabSprinkler/Editor/icon_line.png") as Texture2D, "Line Brush");
    }

    public void DrawUI() // Главный метод отрисовки UI
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
        if (GUILayout.Button(undoIcon, GUILayout.Width(30), GUILayout.Height(30))) Undo.PerformUndo();
        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = settings.brushMode ? Color.gray : Color.white;
        if (GUILayout.Button(brushIcon, GUILayout.Width(30), GUILayout.Height(30))) { settings.brushMode = !settings.brushMode; settings.eraserMode = false; SceneView.RepaintAll(); }
        GUI.backgroundColor = settings.eraserMode ? Color.gray : Color.white;
        if (GUILayout.Button(eraserIcon, GUILayout.Width(30), GUILayout.Height(30))) { settings.eraserMode = !settings.eraserMode; settings.brushMode = false; SceneView.RepaintAll(); }
        GUI.backgroundColor = (!settings.brushMode && !settings.eraserMode) ? Color.gray : Color.white;
        if (GUILayout.Button(cursorIcon, GUILayout.Width(30), GUILayout.Height(30))) { settings.brushMode = false; settings.eraserMode = false; SceneView.RepaintAll(); }
        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    // Рисует кнопки выбора формы кисти
    private void DrawBrushShapeButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUI.backgroundColor = settings.currentBrushShape == 0 ? Color.gray : Color.white;
        if (GUILayout.Button(singleIcon, GUILayout.Width(30), GUILayout.Height(30))) settings.currentBrushShape = 0;
        GUI.backgroundColor = settings.currentBrushShape == 1 ? Color.gray : Color.white;
        if (GUILayout.Button(circleIcon, GUILayout.Width(30), GUILayout.Height(30))) settings.currentBrushShape = 1;
        GUI.backgroundColor = settings.currentBrushShape == 2 ? Color.gray : Color.white;
        if (GUILayout.Button(squareIcon, GUILayout.Width(30), GUILayout.Height(30))) settings.currentBrushShape = 2;
        GUI.backgroundColor = settings.currentBrushShape == 3 ? Color.gray : Color.white;
        if (GUILayout.Button(lineIcon, GUILayout.Width(30), GUILayout.Height(30))) settings.currentBrushShape = 3;
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
            
            // Проверяем, является ли префаб 2D-объектом
            bool is2D = settings.prefabsToSpawn[i] != null && 
                        settings.prefabsToSpawn[i].GetComponentInChildren<SpriteRenderer>() != null;
            
            // Подсвечиваем красным, если это не 2D-объект
            GUI.backgroundColor = (settings.prefabsToSpawn[i] != null && !is2D) ? new Color(1f, 0.5f, 0.5f) : Color.white;
            
            settings.prefabsToSpawn[i] = (GameObject)EditorGUILayout.ObjectField(settings.prefabsToSpawn[i], typeof(GameObject), false);
            
            GUI.backgroundColor = Color.white;
            
            if (GUILayout.Button("X", GUILayout.Width(25))) { settings.prefabsToSpawn.RemoveAt(i); EditorGUILayout.EndHorizontal(); break; }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        
        // Предупреждение о невалидных префабах
        var invalidPrefabs = settings.GetInvalid2DPrefabs();
        if (invalidPrefabs.Count > 0)
        {
            EditorGUILayout.HelpBox($"Warning: {invalidPrefabs.Count} prefab(s) don't have SpriteRenderer component and will be ignored.", MessageType.Warning);
        }
        
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
        if (settings.currentBrushShape == 3)
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
        if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.Z) { Undo.PerformUndo(); e.Use(); }
    }
}