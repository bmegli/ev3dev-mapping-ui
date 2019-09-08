/*
 * Copyright (C) 2019 Bartosz Meglicki <meglickib@gmail.com>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 * This program is distributed "as is" WITHOUT ANY WARRANTY of any
 * kind, whether express or implied; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
*/

/*
	2D (UI elements) drag & scale

	Drag by:
	- left button
	- touch

	Scale by:
	- right button
	- pinch gesture
*/

using UnityEngine;
using UnityEngine.EventSystems;

public class DragMoveScaleHandler : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{	
	public float scaleMouseSpeed = 0.01f;
	public float scalePinchSpeed = 0.01f;

	public float scaleButtonSpeed = 0.01f;

	public float minScale = 0.25f;

	private bool scaling=false;

	private void ValidateScale()
	{
		if(transform.localScale.x < minScale)
			transform.localScale = new Vector3 (minScale, minScale, 1f);
	}

	private float GetPinchDelta()
	{
		float delta=0f;

		if (Input.touchCount == 2)
		{
			Touch touchZero = Input.GetTouch(0);
			Touch touchOne = Input.GetTouch(1);

			Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
			Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

			float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
			float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

			float deltaMagnitudeDiff = (touchDeltaMag - prevTouchDeltaMag);

			delta += deltaMagnitudeDiff;
		}

		return delta;
	}

	private void PinchScale()
	{
		if (!scaling)
			return;

		float scaleDelta = GetPinchDelta ();

		if (scaleDelta == 0f)
			return;

		transform.localScale += new Vector3 (scalePinchSpeed, scalePinchSpeed, 0f) * scaleDelta;
	}

	//To be refactored out to separate class?
	private void ButtonScale()
	{
		float scaleDelta = Input.GetAxis("VideoScale");

		if (scaleDelta == 0f)
			return;

		transform.localScale += new Vector3 (scaleButtonSpeed, scaleButtonSpeed, 0f) * scaleDelta;
	}

	public void Update()
	{
		PinchScale();
		ButtonScale();
		ValidateScale();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		scaling = true;
	}
	public void OnEndDrag(PointerEventData eventData)
	{
		scaling = false;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
			transform.position += (Vector3)(eventData.delta);
		else if (eventData.button == PointerEventData.InputButton.Right)
		{
			transform.localScale += new Vector3 (scaleMouseSpeed, scaleMouseSpeed, 0f) * eventData.delta.y;
			ValidateScale();
		}
	}
}
