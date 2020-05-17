using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasGameInputsHandler : InputsHandler
{
	public GameGrid m_GameGrid;

	private Canvas m_CanvasGame;
	private Canvas m_CanvasGUI;
	private Vector2 m_vInputPosition;

	public void SetCanvasGame(Canvas canvas)
	{
		m_CanvasGame = canvas;
	}

	public void SetCanvasGUI(Canvas canvas)
	{
		m_CanvasGUI = canvas;
	}

	public override void HandleInputs(Vector2 vScreenPosition)
	{
		RectTransform rectTransform = m_CanvasGame.GetComponent<RectTransform>();
		Vector2 vNewScreenPosition = new Vector2(	vScreenPosition.x * rectTransform.rect.width / m_CanvasGUI.GetComponent<RectTransform>().rect.width, 
													vScreenPosition.y * rectTransform.rect.height / m_CanvasGUI.GetComponent<RectTransform>().rect.height);

		m_GameGrid.UpdateInputPosition(vNewScreenPosition);
	}

	public override void InputsStopped()
	{
		m_GameGrid.InputsStopped();
	}

	public Vector2 GetInputPosition()
	{
		return m_vInputPosition;
	}
}
