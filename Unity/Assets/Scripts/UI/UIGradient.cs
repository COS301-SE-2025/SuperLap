using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
  [SerializeField]
  private Color topColor = new Color(1f, 0.3f, 0f, 1f); // Bright orange-red

  [SerializeField]
  private Color bottomColor = new Color(0.6f, 0.1f, 0f, 1f); // Dark orange-red

  [SerializeField]
  private bool useGraphicAlpha = true;

  public override void ModifyMesh(VertexHelper vh)
  {
    if (!IsActive())
      return;

    var count = vh.currentVertCount;
    if (count == 0)
      return;

    var vertices = new UIVertex[count];
    for (int i = 0; i < count; i++)
    {
      vh.PopulateUIVertex(ref vertices[i], i);
    }

    var topY = vertices[0].position.y;
    var bottomY = vertices[0].position.y;

    for (int i = 1; i < count; i++)
    {
      var y = vertices[i].position.y;
      if (y > topY)
        topY = y;
      else if (y < bottomY)
        bottomY = y;
    }

    var height = topY - bottomY;
    for (int i = 0; i < count; i++)
    {
      var vertex = vertices[i];
      var color = Color.Lerp(bottomColor, topColor, (vertex.position.y - bottomY) / height);

      if (useGraphicAlpha)
        color.a *= vertex.color.a;

      vertex.color = color;
      vh.SetUIVertex(vertex, i);
    }
  }

  public void SetColors(Color top, Color bottom)
  {
    topColor = top;
    bottomColor = bottom;
    graphic.SetVerticesDirty();
  }
}