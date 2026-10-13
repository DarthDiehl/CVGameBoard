using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Attach to GridOverlay, a stretched child of CameraFeed.
// Grid origin is top-left. X = column, Y = row.
public class WebcamGrid : MonoBehaviour, IPointerClickHandler
{
    [Min(1)] public int rows = 6;
    [Min(1)] public int columns = 6;
    public Color lineColor = Color.green;
    [Min(1)] public float lineThickness = 3f;

    [Header("Board area within the camera display (0 to 1)")]
    [Tooltip("X/Y are the bottom-left offset; W/H are board width/height.")]
    public Rect boardArea = new Rect(0, 0, 1, 1);

    [Header("Game world mapping (horizontal X/Z plane)")]
    [Tooltip("World position of the board's top-left outer corner.")]
    public Vector3 worldOrigin = Vector3.zero;
    [Min(0.001f)] public float cellSize = 1f;
    [Tooltip("Optional scene object to move to the selected cell center.")]
    public Transform testObject;

    private RectTransform displayRect;
    private RectTransform boardRect;
    private RectTransform highlight;
    private bool rebuild;
    private string status = "Click a grid cell. Origin: top-left.";

    void Start()
    {
        displayRect = GetComponent<RectTransform>();
        if (displayRect == null)
        {
            Debug.LogError("WebcamGrid must be attached to the UI GridOverlay.");
            enabled = false;
            return;
        }
        Image clickArea = GetComponent<Image>();
        if (clickArea == null) clickArea = gameObject.AddComponent<Image>();
        clickArea.color = Color.clear;
        clickArea.raycastTarget = true;
        BuildGrid();
    }

    void OnValidate() { rebuild = true; }

    void Update()
    {
        if (rebuild && displayRect != null) BuildGrid();
    }

    void BuildGrid()
    {
        rebuild = false;
        rows = Mathf.Clamp(rows, 1, 100);
        columns = Mathf.Clamp(columns, 1, 100);
        cellSize = Mathf.Max(0.001f, cellSize);
        lineThickness = Mathf.Max(1f, lineThickness);
        boardArea.x = Mathf.Clamp(boardArea.x, 0f, 0.99f);
        boardArea.y = Mathf.Clamp(boardArea.y, 0f, 0.99f);
        boardArea.width = Mathf.Clamp(boardArea.width, 0.01f, 1f - boardArea.x);
        boardArea.height = Mathf.Clamp(boardArea.height, 0.01f, 1f - boardArea.y);

        if (boardRect != null)
        {
            boardRect.gameObject.SetActive(false);
            Destroy(boardRect.gameObject);
        }
        GameObject holder = new GameObject("GeneratedGrid", typeof(RectTransform));
        holder.transform.SetParent(transform, false);
        boardRect = holder.GetComponent<RectTransform>();
        boardRect.anchorMin = boardArea.min;
        boardRect.anchorMax = boardArea.max;
        boardRect.offsetMin = Vector2.zero;
        boardRect.offsetMax = Vector2.zero;

        highlight = MakeImage("SelectedCell", new Color(1, 1, 0, 0.3f));
        highlight.gameObject.SetActive(false);
        for (int c = 0; c <= columns; c++)
        {
            float x = (float)c / columns;
            MakeLine(new Vector2(x, 0), new Vector2(x, 1), new Vector2(lineThickness, 0));
        }
        for (int r = 0; r <= rows; r++)
        {
            float y = (float)r / rows;
            MakeLine(new Vector2(0, y), new Vector2(1, y), new Vector2(0, lineThickness));
        }
        status = "Click a grid cell. Origin: top-left.";
    }

    // Input is relative to the displayed camera rectangle: (0,0) bottom-left,
    // (1,1) top-right. This is not raw camera pixel data.
    public bool TryDisplayPointToCell(Vector2 point, out Vector2Int cell)
    {
        cell = new Vector2Int(-1, -1);
        if (rows < 1 || columns < 1 || boardArea.width <= 0 || boardArea.height <= 0)
            return false;
        float x = (point.x - boardArea.x) / boardArea.width;
        float y = (point.y - boardArea.y) / boardArea.height;
        if (float.IsNaN(x) || float.IsNaN(y) || x < 0 || y < 0 || x >= 1 || y >= 1)
            return false;
        cell = new Vector2Int(Mathf.FloorToInt(x * columns), rows - 1 - Mathf.FloorToInt(y * rows));
        return true;
    }

    // Call this with Jay's agreed grid coordinates once conventions match.
    public bool TryCellToWorld(int column, int row, out Vector3 position)
    {
        position = Vector3.zero;
        if (column < 0 || row < 0 || column >= columns || row >= rows || cellSize <= 0)
            return false;
        position = worldOrigin + new Vector3((column + 0.5f) * cellSize, 0,
                                             -(row + 0.5f) * cellSize);
        return true;
    }

    public void OnPointerClick(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left || boardRect == null) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            displayRect, data.position, data.pressEventCamera, out Vector2 local)) return;
        Rect bounds = displayRect.rect;
        if (bounds.width <= 0 || bounds.height <= 0) return;
        Vector2 point = new Vector2((local.x - bounds.xMin) / bounds.width,
                                   (local.y - bounds.yMin) / bounds.height);
        if (!TryDisplayPointToCell(point, out Vector2Int cell))
        {
            highlight.gameObject.SetActive(false);
            status = "Outside board: no cell selected.";
            return;
        }
        highlight.anchorMin = new Vector2((float)cell.x / columns, (float)(rows - 1 - cell.y) / rows);
        highlight.anchorMax = new Vector2((float)(cell.x + 1) / columns, (float)(rows - cell.y) / rows);
        highlight.offsetMin = Vector2.zero;
        highlight.offsetMax = Vector2.zero;
        highlight.gameObject.SetActive(true);
        if (!TryCellToWorld(cell.x, cell.y, out Vector3 world)) return;
        if (testObject != null) testObject.position = world;
        status = $"Column {cell.x}, Row {cell.y} | World ({world.x:F2}, {world.y:F2}, {world.z:F2})";
        Debug.Log(status);
    }

    RectTransform MakeImage(string objectName, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(boardRect, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return obj.GetComponent<RectTransform>();
    }

    void MakeLine(Vector2 min, Vector2 max, Vector2 size)
    {
        RectTransform line = MakeImage("GridLine", lineColor);
        line.anchorMin = min;
        line.anchorMax = max;
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = size;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 18;
        style.alignment = TextAnchor.MiddleLeft;
        GUI.Box(new Rect(10, 10, Mathf.Min(650, Screen.width - 20), 48), status, style);
    }
}
