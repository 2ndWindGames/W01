using System;
using System.Collections;
using System.Collections.Generic;
using _01.Scripts.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_EventHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public Action OnClickHandler = null;
    public Action OnPressedHandler = null;
	public Action OnPointerDownHandler = null;
	public Action OnPointerUpHandler = null;

	bool _pressed = false;

	private void Update()
	{
		if (_pressed)
			OnPressedHandler?.Invoke();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (GetComponent<Button>() != null)
			Managers.Sound.Play(Define.Sound.Effect, "SFX/Button_Click", 0.65f);

		OnClickHandler?.Invoke();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		_pressed = true;
		OnPointerDownHandler?.Invoke();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		_pressed = false;
		OnPointerUpHandler?.Invoke();
	}
}
