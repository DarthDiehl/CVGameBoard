using UnityEngine;
using UnityEngine.UI;

public class WebcamGrid : MonoBehaviour
{
    [Min(1)] public int rows = 6;
    [Min(1)] public int columns = 6;
    public Color lineColor = Color.green;
    [Min(1)] public float lineThickness = 3f;

    void Start()
    {
        rows = Mathf.Max(1, rows);
        columns = Mathf.Max(1, columns);

        // Vertical lines
        for (int column = 0; column <= columns; column++)
        {
            float x = (float)column / columns;
            CreateLine(
                new Vector2(x, 0),
                new Vector2(x, 1),
                new Vector2(lineThickness, 0)
            );
        }

        // Horizontal lines
        for (int row = 0; row <= rows; row++)
        {
            float y = (float)row / rows;
            CreateLine(
                new Vector2(0, y),
                new Vector2(1, y),
                new Vector2(0, lineThickness)
            );
        }
    }

    void CreateLine(Vector2 start, Vector2 end, Vector2 size)
    {
        GameObject line = new GameObject(
            "GridLine", typeof(RectTransform), typeof(Image)
        );

        line.transform.SetParent(transform, false);

        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = start;
        rect.anchorMax = end;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image lineImage = line.GetComponent<Image>();
        lineImage.color = lineColor;
        lineImage.raycastTarget = false;
    }
}