using UnityEngine.UI;
public class EmptyGraphic : UnityEngine.UI.Graphic {
	protected override void OnPopulateMesh(VertexHelper vh) {
		vh.Clear();
	}
}