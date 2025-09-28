using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class ResponsiveGridLayout : MonoBehaviour
{
    [Header("Settings")]
    public int columns = 3;
    public Vector2 spacing = new Vector2(10, 10);
    public Vector2 padding = new Vector2(10, 10);
    public float fixedCellHeight = 200f; // 👈 stays constant

    private GridLayoutGroup grid;
    private RectTransform rectTransform;

    void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rectTransform = GetComponent<RectTransform>();

        // Sync inspector values to GridLayout
        grid.spacing = spacing;
    }

    void Update()
    {
        ResizeCells();
    }

    private void ResizeCells()
    {
        float totalWidth = rectTransform.rect.width;

        // account for padding + spacing between cells
        float totalSpacing = spacing.x * (columns - 1);
        float totalPadding = padding.x * 2;

        float usableWidth = totalWidth - totalSpacing - totalPadding;

        float cellWidth = usableWidth / columns;

        grid.cellSize = new Vector2(cellWidth, fixedCellHeight);
    }
}
