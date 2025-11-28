using UnityEngine;
using UnityEditor;

public class PrefabBrush : EditorWindow
{

    float spawnRadius = 1.0f;
    float objectDensity = 0.5f; // Объединенный параметр плотности (0 - редко, 1 - густо)
    GameObject prefabToSpawn;
    bool brushMode = false; // Режим кисти
    bool isMouseDown = false; // Отслеживание зажатия ЛКМ
    float lastSpawnTime = 0f; // Время последнего спавна
    float spawnInterval = 0.1f; // Интервал между спавнами при зажатии ЛКМ
    
    // Иконки для кнопок
    private GUIContent undoIcon;
    private GUIContent brushIcon;
    private GUIContent cursorIcon;

    [MenuItem("Tools/PrefabBrush")]
    public static void ShowWindow()
    {
        GetWindow(typeof(PrefabBrush));
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        LoadIcons();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    private void LoadIcons()
    {
        // Загружаем встроенные иконки Unity или пользовательские из папки
        Texture2D undoTexture = EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_undo.png") as Texture2D;
        Texture2D brushTexture = EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_brush.png") as Texture2D;
        Texture2D cursorTexture = EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_cursor.png") as Texture2D;
        
        // Если иконки не найдены, используем встроенные иконки Unity
        if (undoTexture == null)
            undoTexture = EditorGUIUtility.IconContent("UndoHistory").image as Texture2D;
        if (brushTexture == null)
            brushTexture = EditorGUIUtility.IconContent("EditCollider").image as Texture2D;
        if (cursorTexture == null)
            cursorTexture = EditorGUIUtility.IconContent("ViewToolMove").image as Texture2D;
            
        undoIcon = new GUIContent(undoTexture, "Undo (Ctrl+Z)");
        brushIcon = new GUIContent(brushTexture, "Brush Mode");
        cursorIcon = new GUIContent(cursorTexture, "Normal Cursor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Brush Settings", EditorStyles.boldLabel);
        
        // Квадратные кнопки в верхней части
        EditorGUILayout.BeginHorizontal();
        
        // Кнопка Undo
        EditorGUI.BeginDisabledGroup(!Undo.GetCurrentGroupName().Contains("Spawn Prefab") && Undo.GetCurrentGroup() == 0);
        if (GUILayout.Button(undoIcon, GUILayout.Width(30), GUILayout.Height(30)))
        {
            Undo.PerformUndo();
        }
        EditorGUI.EndDisabledGroup();
        
        // Кнопка режима кисти
        GUI.backgroundColor = brushMode ? Color.gray : Color.white;
        if (GUILayout.Button(brushIcon, GUILayout.Width(30), GUILayout.Height(30)))
        {
            brushMode = !brushMode;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;
        
        // Кнопка выхода из режима кисти (обычный курсор)
        GUI.backgroundColor = !brushMode ? Color.gray : Color.white;
        if (GUILayout.Button(cursorIcon, GUILayout.Width(30), GUILayout.Height(30)))
        {
            brushMode = false;
            isMouseDown = false; // Сбрасываем состояние мыши
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        if (brushMode)
        {
            EditorGUILayout.HelpBox("Brush Mode Active: Click and hold on the scene to spawn prefabs", MessageType.Info);
        }
        
        prefabToSpawn = (GameObject)EditorGUILayout.ObjectField("Prefab to Spawn", prefabToSpawn, typeof(GameObject), false);
        spawnRadius = EditorGUILayout.Slider("Spawn Radius", spawnRadius, 0.5f, 10f);
        objectDensity = EditorGUILayout.Slider("Object Density", objectDensity, 0.1f, 1f);

        if (GUILayout.Button("Spawn Prefabs"))
        {
            SpawnPrefabs(Vector3.zero);
        }

        // Обработка горячей клавиши Ctrl+Z
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.control && e.keyCode == KeyCode.Z)
        {
            Undo.PerformUndo();
            e.Use();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!brushMode) return;

        Event e = Event.current;
        
        // Отображение круга радиуса на сцене
        if (e.type == EventType.Repaint)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Handles.color = new Color(0, 1, 0, 0.3f);
                Handles.DrawSolidDisc(hit.point, Vector3.forward, spawnRadius);
                Handles.color = Color.green;
                Handles.DrawWireDisc(hit.point, Vector3.forward, spawnRadius);
            }
            else
            {
                // Если нет коллайдера, используем плоскость z=0
                Plane plane = new Plane(Vector3.forward, Vector3.zero);
                if (plane.Raycast(ray, out float distance))
                {
                    Vector3 point = ray.GetPoint(distance);
                    Handles.color = new Color(0, 1, 0, 0.3f);
                    Handles.DrawSolidDisc(point, Vector3.forward, spawnRadius);
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(point, Vector3.forward, spawnRadius);
                }
            }
        }

        // Обработка нажатия ЛКМ
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            isMouseDown = true;
            lastSpawnTime = 0f; // Сбрасываем время для немедленного спавна
            e.Use();
        }
        
        // Обработка отпускания ЛКМ
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            isMouseDown = false;
            e.Use();
        }
        
        // Спавн префабов при зажатой ЛКМ
        if (isMouseDown && e.type == EventType.MouseDrag || (isMouseDown && e.type == EventType.MouseMove))
        {
            float currentTime = (float)EditorApplication.timeSinceStartup;
            
            // Проверяем, прошел ли достаточный интервал времени
            if (currentTime - lastSpawnTime >= spawnInterval)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Vector3 spawnCenter = Vector3.zero;
                
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    spawnCenter = hit.point;
                }
                else
                {
                    // Если нет коллайдера, используем плоскость z=0
                    Plane plane = new Plane(Vector3.forward, Vector3.zero);
                    if (plane.Raycast(ray, out float distance))
                    {
                        spawnCenter = ray.GetPoint(distance);
                    }
                }

                SpawnPrefabs(spawnCenter);
                lastSpawnTime = currentTime;
            }
            
            e.Use();
        }
        
        // Также оставляем обработку одиночного клика для первого спавна
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 spawnCenter = Vector3.zero;
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                spawnCenter = hit.point;
            }
            else
            {
                // Если нет коллайдера, используем плоскость z=0
                Plane plane = new Plane(Vector3.forward, Vector3.zero);
                if (plane.Raycast(ray, out float distance))
                {
                    spawnCenter = ray.GetPoint(distance);
                }
            }

            SpawnPrefabs(spawnCenter);
            e.Use();
        }

        // Обновляем курсор
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        SceneView.RepaintAll();
    }

    private System.Collections.Generic.List<Vector3> GetExistingPrefabPositions(Vector3 center, float radius)
    {
        var existingPositions = new System.Collections.Generic.List<Vector3>();
        
        if (prefabToSpawn == null)
            return existingPositions;
        
        // Находим все объекты на сцене с таким же именем, как у префаба
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        string prefabName = prefabToSpawn.name;
        
        foreach (GameObject obj in allObjects)
        {
            // Проверяем, соответствует ли имя объекта имени префаба
            if (obj.name == prefabName || obj.name.StartsWith(prefabName))
            {
                // Проверяем, находится ли объект в радиусе действия кисти
                float distance = Vector3.Distance(obj.transform.position, center);
                if (distance <= radius * 2f) // Увеличиваем радиус проверки для лучшего эффекта
                {
                    existingPositions.Add(obj.transform.position);
                }
            }
        }
        
        return existingPositions;
    }

    /// <summary>
    /// Настраивает сортировку для 2D вида сверху: объекты с меньшим Y отображаются поверх объектов с большим Y
    /// </summary>
    private void SetupSortingOrder(GameObject obj)
    {
        // Получаем все SpriteRenderer компоненты (включая дочерние объекты)
        SpriteRenderer[] spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>(true);
        
        foreach (SpriteRenderer sr in spriteRenderers)
        {
            // Устанавливаем sorting order на основе позиции Y
            // Чем меньше Y (ниже на карте), тем выше sorting order (отображается сверху)
            // Умножаем на -100 для создания достаточного диапазона значений
            sr.sortingOrder = Mathf.RoundToInt(-obj.transform.position.y * 100f);
        }
        
        // Если нет SpriteRenderer, проверяем наличие других рендереров
        if (spriteRenderers.Length == 0)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                // Для других типов рендереров используем sortingOrder через материал (если возможно)
                if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_SortingOrder"))
                {
                    renderer.sharedMaterial.SetFloat("_SortingOrder", -obj.transform.position.y * 100f);
                }
            }
        }
    }

    private void SpawnPrefabs(Vector3 center)
    {
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("Please assign a prefab to spawn.");
            return;
        }
        
        // Вычисляем параметры на основе плотности
        // Чем выше плотность, тем больше объектов и меньше минимальное расстояние
        float area = Mathf.PI * spawnRadius * spawnRadius;
        int objectCount = Mathf.RoundToInt(area * objectDensity * 10); // 10 - коэффициент масштабирования
        float minDistance = Mathf.Lerp(5f, 0.3f, objectDensity); // При низкой плотности - большое расстояние, при высокой - маленькое
        
        // Получаем позиции существующих префабов на сцене
        var existingPositions = GetExistingPrefabPositions(center, spawnRadius);
        
        // Объединяем существующие позиции с новыми
        var positions = new System.Collections.Generic.List<Vector3>(existingPositions);
        
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = objectCount * 20; // ограничение попыток
        int skippedDueToExisting = 0;
        
        while (spawned < objectCount && attempts < maxAttempts)
        {
            Vector3 randomPosition = Random.insideUnitSphere * spawnRadius;
            randomPosition.z = 0; // Keep on ground level
            randomPosition += center; // Смещаем относительно центра
            
            bool tooClose = false;
            foreach (var pos in positions)
            {
                if (Vector3.Distance(pos, randomPosition) < minDistance)
                {
                    tooClose = true;
                    // Проверяем, существующий ли это объект
                    if (existingPositions.Contains(pos))
                    {
                        skippedDueToExisting++;
                    }
                    break;
                }
            }
            if (!tooClose)
            {
                GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);
                newObject.transform.position = randomPosition;
                newObject.name = prefabToSpawn.name;
                
                // Настраиваем сортировку для 2D вида сверху
                SetupSortingOrder(newObject);
                
                Undo.RegisterCreatedObjectUndo(newObject, "Spawn Prefab");
                positions.Add(randomPosition);
                spawned++;
            }
            attempts++;
        }
        
        // Улучшенный вывод информации (только для режима кнопки, чтобы не спамить логами)
        if (!isMouseDown)
        {
            if (spawned < objectCount)
            {
                string message = $"Spawned {spawned}/{objectCount} objects.";
                if (existingPositions.Count > 0)
                {
                    message += $" Found {existingPositions.Count} existing prefabs nearby.";
                }
                Debug.LogWarning(message);
            }
            else
            {
                string message = $"Successfully spawned {spawned} objects with density {objectDensity:F2}.";
                if (existingPositions.Count > 0)
                {
                    message += $" Avoided {existingPositions.Count} existing prefabs.";
                }
                Debug.Log(message);
            }
        }
    }
}
