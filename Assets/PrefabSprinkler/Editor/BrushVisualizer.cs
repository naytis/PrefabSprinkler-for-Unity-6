using UnityEngine;
using UnityEditor;

public class BrushVisualizer // Класс для визуализации кисти в Scene View
{
    private readonly BrushSettings settings; // Ссылка на настройки кисти
    
    public BrushVisualizer(BrushSettings settings) // Конструктор класса
    {
        this.settings = settings;
    }
    
    // Главный метод для отрисовки визуализации кисти в указанной точке
    public void DrawBrushPreview(Vector3 point)
    {
        Color mainColor = settings.eraserMode ? Color.red : Color.green; // Цвет в зависимости от режима
        Color fillColor = settings.eraserMode ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);
        
        switch (settings.currentBrushShape) // Выбираем метод отрисовки по форме кисти
        {
            case 0: DrawCrosshair(point, mainColor); break;
            case 1: DrawCircle(point, mainColor, fillColor); break;
            case 2: DrawSquare(point, mainColor, fillColor); break;
            case 3: DrawLine(point, mainColor, fillColor); break;
        }
    }
    
    // Рисует перекрестье для режима одиночного объекта
    private void DrawCrosshair(Vector3 point, Color color)
    {
        float crosshairSize = 0.3f;
        
        Vector3 hLineStart = point + new Vector3(-crosshairSize, 0, 0); // Горизонтальная линия
        Vector3 hLineEnd = point + new Vector3(crosshairSize, 0, 0);
        Vector3 vLineStart = point + new Vector3(0, -crosshairSize, 0); // Вертикальная линия
        Vector3 vLineEnd = point + new Vector3(0, crosshairSize, 0);
        
        Handles.color = color;
        Handles.DrawLine(hLineStart, hLineEnd, 2f);
        Handles.DrawLine(vLineStart, vLineEnd, 2f);
        Handles.DrawSolidDisc(point, Vector3.forward, 0.05f); // Центральная точка
    }
    
    // Рисует круглую кисть с заливкой и контуром
    private void DrawCircle(Vector3 point, Color mainColor, Color fillColor)
    {
        Handles.color = fillColor;
        Handles.DrawSolidDisc(point, Vector3.forward, settings.spawnRadius);
        Handles.color = mainColor;
        Handles.DrawWireDisc(point, Vector3.forward, settings.spawnRadius);
    }
    
    // Рисует квадратную кисть с заливкой и контуром
    private void DrawSquare(Vector3 point, Color mainColor, Color fillColor)
    {
        Vector3[] squarePoints = new Vector3[5]; // Массив точек для квадрата
        squarePoints[0] = point + new Vector3(-settings.spawnRadius, -settings.spawnRadius, 0);
        squarePoints[1] = point + new Vector3(settings.spawnRadius, -settings.spawnRadius, 0);
        squarePoints[2] = point + new Vector3(settings.spawnRadius, settings.spawnRadius, 0);
        squarePoints[3] = point + new Vector3(-settings.spawnRadius, settings.spawnRadius, 0);
        squarePoints[4] = squarePoints[0]; // Замыкаем контур
        
        Handles.DrawSolidRectangleWithOutline(squarePoints, fillColor, mainColor);
    }
    
    // Рисует линейную кисть с учетом угла наклона
    private void DrawLine(Vector3 point, Color mainColor, Color fillColor)
    {
        float angleRad = settings.lineAngle * Mathf.Deg2Rad; // Направление линии с учетом угла
        Vector3 direction = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0);
        
        Vector3 lineStart = point - direction * (settings.spawnRadius / 2); // Начало и конец линии
        Vector3 lineEnd = point + direction * (settings.spawnRadius / 2);
        
        float lineWidth = 0.15f; // Ширина линии для визуализации
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0) * lineWidth;
        
        Vector3[] lineRectPoints = new Vector3[5]; // Прямоугольник вдоль линии
        lineRectPoints[0] = lineStart - perpendicular;
        lineRectPoints[1] = lineEnd - perpendicular;
        lineRectPoints[2] = lineEnd + perpendicular;
        lineRectPoints[3] = lineStart + perpendicular;
        lineRectPoints[4] = lineRectPoints[0]; // Замыкаем контур
        
        Handles.DrawSolidRectangleWithOutline(lineRectPoints, fillColor, mainColor);
    }
}
