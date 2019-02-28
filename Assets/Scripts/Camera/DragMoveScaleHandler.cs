using UnityEngine;
using UnityEngine.EventSystems;

public class DragMoveScaleHandler : MonoBehaviour, IDragHandler
{
	public void OnDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
			transform.position += (Vector3)(eventData.delta);
		else if (eventData.button == PointerEventData.InputButton.Right)
		{
			transform.localScale += new Vector3 (0.01f, 0.01f, 0f) * eventData.delta.y;
			if (transform.localScale.x < 0.2)
				transform.localScale = new Vector3 (0.2f, 0.2f, 1f);
		}
	}
}
