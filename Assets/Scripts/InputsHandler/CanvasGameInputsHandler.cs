using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasGameInputsHandler : InputsHandler
{
	private GamePlaySelection m_GamePlaySelection;
	private Canvas m_CanvasGame;
	private Canvas m_CanvasGUI;
	private CameraInputsHandler m_CameraInputsHandler;
	private Vector2 m_vInputPosition;
	private Vector2 m_vInputPositionTransformed;

	public void SetGamePlaySelection(GamePlaySelection gamePlaySelection)
	{
		m_GamePlaySelection = gamePlaySelection;
	}

	public void SetCanvasGame(Canvas canvas)
	{
		m_CanvasGame = canvas;
	}

	public void SetCanvasGUI(Canvas canvas)
	{
		m_CanvasGUI = canvas;
	}

	public void SetCameraInputsHandler(CameraInputsHandler cameraInputsHandler)
	{
		m_CameraInputsHandler = cameraInputsHandler;
	}

	public override void HandleInputs(Vector2 vScreenPosition)
	{
		m_vInputPosition = vScreenPosition;
		RectTransform rectTransform = m_CanvasGame.GetComponent<RectTransform>();
		float fZoomRatio = m_CameraInputsHandler.GetZoomRatio();
		Vector2 vScreenPositionWithModifiers = m_CameraInputsHandler.GetPanning() + vScreenPosition;
		Vector2 vNewScreenPosition = new Vector2(	vScreenPositionWithModifiers.x * rectTransform.rect.width / m_CanvasGUI.GetComponent<RectTransform>().rect.width,
													vScreenPositionWithModifiers.y * rectTransform.rect.height / m_CanvasGUI.GetComponent<RectTransform>().rect.height);
		m_vInputPositionTransformed = vNewScreenPosition;
		m_GamePlaySelection.UpdateInputPosition(vNewScreenPosition);
	}

	public override void InputsStopped()
	{
		m_GamePlaySelection.InputsStopped();
	}
}
