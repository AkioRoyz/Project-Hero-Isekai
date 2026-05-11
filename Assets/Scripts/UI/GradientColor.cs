using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[AddComponentMenu("UI/Effects/Gradient")]
public class GradientColor : BaseMeshEffect
{
    public Color color1 = Color.white;
    public Color color2 = Color.black;
    public bool horizontal = true;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> vertexList = new List<UIVertex>();
        vh.GetUIVertexStream(vertexList);

        for (int i = 0; i < vertexList.Count; i++)
        {
            UIVertex vertex = vertexList[i];

            // Расчет градиента на основе позиции вершины
            float t = horizontal ?
                (vertex.position.x - 0) / 100f : // Упрощенно
                (vertex.position.y - 0) / 100f;

            vertex.color = Color.Lerp(color1, color2, t);
            vertexList[i] = vertex;
        }

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertexList);
    }
}
