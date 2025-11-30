using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabBrush : EditorWindow
{
    float spawnRadius = 1.0f;
    float objectDensity = 0.5f; // Объединенный параметр плотности (0 - редко, 1 - густо)
    List<GameObject> prefabsToSpawn = new List<GameObject>(); // Список префабов для размещения
    bool brushMode = false; // Режим кисти
    bool eraserMode = false; // Режим стирания
    bool isMouseDown = false; // Отслеживание зажатия ЛКМ
    float lastSpawnTime = 0f; // Время последнего спавна
    float spawnInterval = 0.1f; // Интервал между спавнами при зажатии ЛКМ
    float lineAngle = 0f; // Угол наклона линии в градусах
    bool randomFlipY = false; // Случайное отзеркаливание по оси Y
    
    // Типы кисти: 0 = Single, 1 = Circle, 2 = Square, 3 = Line
    int currentBrushShape = 1; // По умолчанию круглая кисть
    
    // Для отображения списка префабов
    Vector2 scrollPosition;
    
    // Иконки для кнопок
    private GUIContent undoIcon;
    private GUIContent brushIcon;
    private GUIContent eraserIcon;
    private GUIContent cursorIcon;
    private GUIContent singleIcon;
    private GUIContent circleIcon;
    private GUIContent squareIcon;
    private GUIContent lineIcon;
    private GUIContent flipIcon;

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
        Texture2D eraserTexture = EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_eraser.png") as Texture2D;
        Texture2D cursorTexture = EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_cursor.png") as Texture2D;
        Texture2D singleTexture = EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_single.png") as Texture2D;
        Texture2D circleTexture = EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_circle.png") as Texture2D;
        Texture2D squareTexture = EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_square.png") as Texture2D;
        Texture2D lineTexture = EditorGUIUtility.Load("Assets/PrefabBrush/Editor/icon_line.png") as Texture2D;
        

        undoIcon = new GUIContent(undoTexture, "Undo");
        brushIcon = new GUIContent(brushTexture, "Brush Mode");
        eraserIcon = new GUIContent(eraserTexture, "Eraser Mode");
        cursorIcon = new GUIContent(cursorTexture, "Normal Cursor");
        singleIcon = new GUIContent(singleTexture, "Single Object");
        circleIcon = new GUIContent(circleTexture, "Circle Brush");
        squareIcon = new GUIContent(squareTexture, "Square Brush");
        lineIcon = new GUIContent(lineTexture, "Line Brush");
    }

    private void OnGUI()
    {
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
            eraserMode = false;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;
        
        // Кнопка режима ластика
        GUI.backgroundColor = eraserMode ? Color.gray : Color.white;
        if (GUILayout.Button(eraserIcon, GUILayout.Width(30), GUILayout.Height(30)))
        {
            eraserMode = !eraserMode;
            brushMode = false;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;
        
        // Кнопка выхода из режима кисти (обычный курсор)
        GUI.backgroundColor = (!brushMode && !eraserMode) ? Color.gray : Color.white;
        if (GUILayout.Button(cursorIcon, GUILayout.Width(30), GUILayout.Height(30)))
        {
            brushMode = false;
            eraserMode = false;
            isMouseDown = false; // Сбрасываем состояние мыши
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        GUILayout.Label(eraserMode ? "Eraser Mode" : "Brush Mode", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(5);
        
        // Кнопки выбора формы кисти
        EditorGUILayout.BeginHorizontal();
        
        // Кнопка одиночного объекта
        GUI.backgroundColor = currentBrushShape == 0 ? Color.gray : Color.white;
        if (GUILayout.Button(singleIcon, GUILayout.Width(30), GUILayout.Height(30)))
        {
            currentBrushShape = 0;
        }
        GUI.backgroundColor = Color.white;
        
        // Кнопка круглой кисти
        GUI.backgroundColor = currentBrushShape == 1 ? Color.gray : Color.white;
        if (GUILayout.Button(circleIcon, GUILayout.Width(30), GUILayout.Height(30)))
        {
            currentBrushShape = 1;
        }
        GUI.backgroundColor = Color.white;
        
        // Кнопка квадратной кисти
        GUI.backgroundColor = currentBrushShape == 2 ? Color.gray : Color.white;
        if (GUILayout.Button(squareIcon, GUILayout.Width(30), GUILayout.Height(30)))
        {
            currentBrushShape = 2;
        }
        GUI.backgroundColor = Color.white;
        
        // Кнопка линейной кисти
        GUI.backgroundColor = currentBrushShape == 3 ? Color.gray : Color.white;
        if (GUILayout.Button(lineIcon, GUILayout.Width(30), GUILayout.Height(30)))
        {
            currentBrushShape = 3;
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        // Список префабов с возможностью добавления/удаления
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Prefabs to Spawn", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(100));
        
        for (int i = 0; i < prefabsToSpawn.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            prefabsToSpawn[i] = (GameObject)EditorGUILayout.ObjectField(prefabsToSpawn[i], typeof(GameObject), false);
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                prefabsToSpawn.RemoveAt(i);
                EditorGUILayout.EndHorizontal(); // Закрываем horizontal перед break
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Prefab"))
        {
            prefabsToSpawn.Add(null);
        }
        if (GUILayout.Button("Clear All") && prefabsToSpawn.Count > 0)
        {
            prefabsToSpawn.Clear();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        GUILayout.Label(eraserMode ? "Eraser Settings" : "Brush Settings", EditorStyles.boldLabel);
        
        // Чекбокс случайного отзеркаливания только для режима кисти
        if (!eraserMode)
        {
            randomFlipY = EditorGUILayout.Toggle("Random Mirroring", randomFlipY);
        }
        
        // Для линейной кисти показываем длину линии вместо радиуса
        if (currentBrushShape == 3)
        {
            spawnRadius = EditorGUILayout.Slider(eraserMode ? "Eraser Length" : "Line Length", spawnRadius, 0.5f, 10f);
            lineAngle = EditorGUILayout.Slider("Line Angle", lineAngle, 0f, 180f);
        }
        else if (currentBrushShape != 0) // Показываем размер кисти для круга и квадрата
        {
            spawnRadius = EditorGUILayout.Slider(eraserMode ? "Eraser Size" : "Brush Size", spawnRadius, 0.5f, 10f);
        }
        
        // Показываем плотность только для режима кисти и всех режимов кроме одиночного
        if (!eraserMode && currentBrushShape != 0)
        {
            objectDensity = EditorGUILayout.Slider("Object Density", objectDensity, 0.1f, 1f);
        }

        if (Event.current.type == EventType.Repaint)
        {
            GUI.FocusControl(null);
        }
        
        // Информационное сообщение о режиме кисти в нижней части окна
        if (brushMode || eraserMode)
        {
            EditorGUILayout.Space(10);
            string shapeText = currentBrushShape == 0 ? "Single Object" : 
                             currentBrushShape == 1 ? "Circle" : 
                             currentBrushShape == 2 ? "Square" : "Line";
            
            if (eraserMode)
            {
                EditorGUILayout.HelpBox($"Eraser Mode Active ({shapeText}): Click and hold on the scene to erase prefabs", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"Brush Mode Active ({shapeText}): Click and hold on the scene to spawn prefabs", MessageType.Info);
            }
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
        if (!brushMode && !eraserMode) return;

        Event e = Event.current;
        
        // Отображение формы кисти на сцене
        if (e.type == EventType.Repaint)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 point = Vector3.zero;
            bool hasPoint = false;
            
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                point = hit.point;
                hasPoint = true;
            }
            else
            {
                // Если нет коллайдера, используем плоскость z=0
                Plane plane = new Plane(Vector3.forward, Vector3.zero);
                if (plane.Raycast(ray, out float distance))
                {
                    point = ray.GetPoint(distance);
                    hasPoint = true;
                }
            }
            
            if (hasPoint)
            {
                // Используем красный цвет для ластика, зелёный для кисти
                Color mainColor = eraserMode ? Color.red : Color.green;
                Color fillColor = eraserMode ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);
                
                if (currentBrushShape == 0) // Single
                {
                    // Отображаем перекрестье для одиночного объекта
                    float crosshairSize = 0.3f;
                    
                    // Горизонтальная линия
                    Vector3 hLineStart = point + new Vector3(-crosshairSize, 0, 0);
                    Vector3 hLineEnd = point + new Vector3(crosshairSize, 0, 0);
                    
                    // Вертикальная линия
                    Vector3 vLineStart = point + new Vector3(0, -crosshairSize, 0);
                    Vector3 vLineEnd = point + new Vector3(0, crosshairSize, 0);
                    
                    // Рисуем перекрестье
                    Handles.color = mainColor;
                    Handles.DrawLine(hLineStart, hLineEnd, 2f);
                    Handles.DrawLine(vLineStart, vLineEnd, 2f);
                    
                    // Рисуем центральную точку
                    Handles.DrawSolidDisc(point, Vector3.forward, 0.05f);
                }
                else if (currentBrushShape == 1) // Circle
                {
                    // Отображаем круг с заливкой и контуром
                    Handles.color = fillColor;
                    Handles.DrawSolidDisc(point, Vector3.forward, spawnRadius);
                    Handles.color = mainColor;
                    Handles.DrawWireDisc(point, Vector3.forward, spawnRadius);
                }
                else if (currentBrushShape == 2) // Square
                {
                    // Отображаем квадрат с заливкой и контуром
                    Vector3[] squarePoints = new Vector3[5];
                    squarePoints[0] = point + new Vector3(-spawnRadius, -spawnRadius, 0);
                    squarePoints[1] = point + new Vector3(spawnRadius, -spawnRadius, 0);
                    squarePoints[2] = point + new Vector3(spawnRadius, spawnRadius, 0);
                    squarePoints[3] = point + new Vector3(-spawnRadius, spawnRadius, 0);
                    squarePoints[4] = squarePoints[0];
                    
                    Handles.DrawSolidRectangleWithOutline(
                        squarePoints,
                        fillColor,
                        mainColor
                    );
                }
                else // Line
                {
                    // Вычисляем направление линии с учетом угла
                    float angleRad = lineAngle * Mathf.Deg2Rad;
                    Vector3 direction = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
                    
                    // Отображаем линию с учетом угла
                    Vector3 lineStart = point - direction * (spawnRadius / 2);
                    Vector3 lineEnd = point + direction * (spawnRadius / 2);
                    
                    // Ширина линии для визуализации (уменьшена в два раза)
                    float lineWidth = 0.15f;
                    Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * lineWidth;
                    
                    // Создаем прямоугольник вдоль линии
                    Vector3[] lineRectPoints = new Vector3[5];
                    lineRectPoints[0] = lineStart - perpendicular;
                    lineRectPoints[1] = lineEnd - perpendicular;
                    lineRectPoints[2] = lineEnd + perpendicular;
                    lineRectPoints[3] = lineStart + perpendicular;
                    lineRectPoints[4] = lineRectPoints[0];
                    
                    // Рисуем с заливкой и контуром
                    Handles.DrawSolidRectangleWithOutline(
                        lineRectPoints,
                        fillColor,
                        mainColor
                    );
                }
            }
        }

        // Обработка нажатия ЛКМ
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            isMouseDown = true;
            lastSpawnTime = 0f; // Сбрасываем время для немедленного спавна
            
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

            if (eraserMode)
            {
                EraseObjects(spawnCenter);
            }
            else
            {
                SpawnPrefabs(spawnCenter);
            }
            e.Use();
        }
        
        // Обработка отпускания ЛКМ
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            isMouseDown = false;
            e.Use();
        }
        
        // Спавн префабов при зажатой ЛКМ
        if (isMouseDown && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
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

                if (eraserMode)
                {
                    EraseObjects(spawnCenter);
                }
                else
                {
                    SpawnPrefabs(spawnCenter);
                }
                lastSpawnTime = currentTime;
            }
            
            e.Use();
        }

        // Обновляем курсор
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        SceneView.RepaintAll();
    }

    private void EraseObjects(Vector3 center)
    {
        if (prefabsToSpawn.Count == 0 || prefabsToSpawn.TrueForAll(p => p == null))
        {
            Debug.LogWarning("Please add at least one prefab to erase.");
            return;
        }
        
        // Получаем список только непустых префабов
        List<GameObject> validPrefabs = prefabsToSpawn.FindAll(p => p != null);
        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning("Please assign prefabs in the list.");
            return;
        }
        
        // Находим все объекты на сцене
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        List<GameObject> objectsToDelete = new List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            // Проверяем, соответствует ли имя объекта имени любого из префабов
            bool matchesPrefab = false;
            foreach (GameObject prefab in validPrefabs)
            {
                if (prefab != null && (obj.name == prefab.name || obj.name.StartsWith(prefab.name)))
                {
                    matchesPrefab = true;
                    break;
                }
            }
            
            if (!matchesPrefab)
                continue;
            
            // Проверяем, находится ли объект в области действия ластика
            bool inArea = false;
            
            if (currentBrushShape == 0) // Single - удаляем ближайший объект
            {
                float distance = Vector3.Distance(obj.transform.position, center);
                if (distance < 0.5f) // Небольшой радиус для точного удаления
                {
                    objectsToDelete.Add(obj);
                }
            }
            else if (currentBrushShape == 1) // Circle
            {
                float distance = Vector3.Distance(obj.transform.position, center);
                if (distance <= spawnRadius)
                {
                    inArea = true;
                }
            }
            else if (currentBrushShape == 2) // Square
            {
                Vector3 localPos = obj.transform.position - center;
                if (Mathf.Abs(localPos.x) <= spawnRadius && Mathf.Abs(localPos.y) <= spawnRadius)
                {
                    inArea = true;
                }
            }
            else if (currentBrushShape == 3) // Line
            {
                // Вычисляем направление линии с учетом угла
                float angleRad = lineAngle * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
                Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
                
                Vector3 lineStart = center - direction * (spawnRadius / 2);
                Vector3 lineEnd = center + direction * (spawnRadius / 2);
                
                // Проверяем расстояние до линии
                Vector3 toPoint = obj.transform.position - lineStart;
                float projection = Vector3.Dot(toPoint, direction);
                
                if (projection >= 0 && projection <= spawnRadius)
                {
                    float perpendicularDistance = Mathf.Abs(Vector3.Dot(toPoint, perpendicular));
                    if (perpendicularDistance <= 0.3f) // Ширина линии ластика
                    {
                        inArea = true;
                    }
                }
            }
            
            if (inArea)
            {
                objectsToDelete.Add(obj);
            }
        }
        
        // Удаляем объекты с поддержкой Undo
        if (currentBrushShape == 0 && objectsToDelete.Count > 0)
        {
            // Для одиночного режима удаляем только ближайший объект
            GameObject closest = objectsToDelete[0];
            float minDist = Vector3.Distance(closest.transform.position, center);
            
            foreach (GameObject obj in objectsToDelete)
            {
                float dist = Vector3.Distance(obj.transform.position, center);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = obj;
                }
            }
            
            Undo.DestroyObjectImmediate(closest);
        }
        else
        {
            // Для остальных режимов удаляем все объекты в области
            foreach (GameObject obj in objectsToDelete)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }
    }

    private System.Collections.Generic.List<Vector3> GetExistingPrefabPositions(Vector3 center, float radius)
    {
        var existingPositions = new System.Collections.Generic.List<Vector3>();
        
        if (prefabsToSpawn.Count == 0)
            return existingPositions;
        
        // Находим все объекты на сцене с такими же именами, как у префабов
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            // Проверяем, соответствует ли имя объекта имени любого из префабов
            foreach (GameObject prefab in prefabsToSpawn)
            {
                if (prefab != null && (obj.name == prefab.name || obj.name.StartsWith(prefab.name)))
                {
                    // Проверяем, находится ли объект в радиусе действия кисти
                    float distance = Vector3.Distance(obj.transform.position, center);
                    if (distance <= radius * 2f) // Увеличиваем радиус проверки для лучшего эффекта
                    {
                        existingPositions.Add(obj.transform.position);
                        break; // Выходим из внутреннего цикла, чтобы не добавлять позицию дважды
                    }
                }
            }
        }
        
        return existingPositions;
    }


    // Настраивает сортировку для 2D вида сверху: объекты с меньшим Y отображаются поверх объектов с большим Y
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
        if (prefabsToSpawn.Count == 0 || prefabsToSpawn.TrueForAll(p => p == null))
        {
            Debug.LogWarning("Please add at least one prefab to spawn.");
            return;
        }
        
        // Получаем список только непустых префабов
        List<GameObject> validPrefabs = prefabsToSpawn.FindAll(p => p != null);
        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning("Please assign prefabs in the list.");
            return;
        }
        
        // Для одиночного объекта просто создаем один случайный объект в центре
        if (currentBrushShape == 0) // Single
        {
            GameObject randomPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(randomPrefab);
            newObject.transform.position = center;
            newObject.name = randomPrefab.name;
            
            // Применяем случайное отзеркаливание по оси Y, если активировано
            if (randomFlipY && Random.value > 0.5f)
            {
                Vector3 scale = newObject.transform.localScale;
                scale.x *= -1;
                newObject.transform.localScale = scale;
            }
            
            SetupSortingOrder(newObject);
            Undo.RegisterCreatedObjectUndo(newObject, "Spawn Prefab");
            return;
        }
        
        // Вычисляем параметры на основе плотности
        float area;
        if (currentBrushShape == 1) // Circle
        {
            area = Mathf.PI * spawnRadius * spawnRadius;
        }
        else if (currentBrushShape == 2) // Square
        {
            area = (spawnRadius * 2) * (spawnRadius * 2);
        }
        else // Line
        {
            area = spawnRadius; // Для линии используем длину
        }
        
        int objectCount = Mathf.RoundToInt(area * objectDensity * 10);
        float minDistance = Mathf.Lerp(5f, 0.3f, objectDensity);
        
        // Получаем позиции существующих префабов на сцене
        var existingPositions = GetExistingPrefabPositions(center, spawnRadius);
        
        // Объединяем существующие позиции с новыми
        var positions = new System.Collections.Generic.List<Vector3>(existingPositions);
        
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = objectCount * 20;
        
        while (spawned < objectCount && attempts < maxAttempts)
        {
            Vector3 randomPosition;
            
            if (currentBrushShape == 1) // Circle
            {
                // Круглая форма
                randomPosition = Random.insideUnitSphere * spawnRadius;
                randomPosition.z = 0;
            }
            else if (currentBrushShape == 2) // Square
            {
                // Квадратная форма
                randomPosition = new Vector3(
                    Random.Range(-spawnRadius, spawnRadius),
                    Random.Range(-spawnRadius, spawnRadius),
                    0
                );
            }
            else // Line
            {
                // Линейная форма с учетом угла
                float angleRad = lineAngle * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
                Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0);
                
                // Случайная позиция вдоль линии
                float alongLine = Random.Range(-spawnRadius / 2, spawnRadius / 2);
                float acrossLine = Random.Range(-minDistance * 0.5f, minDistance * 0.5f); // Небольшое отклонение перпендикулярно
                
                randomPosition = direction * alongLine + perpendicular * acrossLine;
            }
            
            randomPosition += center;
            
            // Добавляем случайную вариацию к минимальному расстоянию (от 0.7 до 1.3 от базового значения)
            float randomizedMinDistance = minDistance * Random.Range(0.7f, 1.3f);
            
            bool tooClose = false;
            foreach (var pos in positions)
            {
                if (Vector3.Distance(pos, randomPosition) < randomizedMinDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (!tooClose)
            {
                // Выбираем случайный префаб из списка
                GameObject randomPrefab = validPrefabs[Random.Range(0, validPrefabs.Count)];
                GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(randomPrefab);
                newObject.transform.position = randomPosition;
                newObject.name = randomPrefab.name;
                
                // Применяем случайное отзеркаливание по оси Y, если активировано
                if (randomFlipY && Random.value > 0.5f)
                {
                    Vector3 scale = newObject.transform.localScale;
                    scale.x *= -1;
                    newObject.transform.localScale = scale;
                }
                
                // Настраиваем сортировку для 2D вида сверху
                SetupSortingOrder(newObject);
                
                Undo.RegisterCreatedObjectUndo(newObject, "Spawn Prefab");
                positions.Add(randomPosition);
                spawned++;
            }
            attempts++;
        }
    }
}
