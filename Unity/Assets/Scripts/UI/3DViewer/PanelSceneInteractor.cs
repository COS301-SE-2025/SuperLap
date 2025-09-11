using UnityEngine;
using UnityEngine.EventSystems;

public class PanelSceneInteractor : MonoBehaviour, IPointerClickHandler
{
    public Camera sceneCamera;

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 localCursor;
        RectTransform rectTransform = GetComponent<RectTransform>();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
            return;

        // Convert panel space → normalized UV
        Vector2 normalized = Rect.PointToNormalized(rectTransform.rect, localCursor);

        // Convert normalized → pixel coordinates in render texture
        Vector2 textureCoord = new Vector2(normalized.x * sceneCamera.pixelWidth, normalized.y * sceneCamera.pixelHeight);

        // Create ray
        Ray ray = sceneCamera.ScreenPointToRay(textureCoord);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Hit " + hit.collider.name);
            // Example: highlight or interact
            // hit.collider.GetComponent<Renderer>().material.color = Color.red;
        }
    }
}
