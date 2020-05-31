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

		//
		// Ratio between two canvas size. Simplification here as initial orthographic size of 
		// game camera = size of game canvas. And zoom modifies GameCamera ortho size.
		Vector2 vRatioCanvas = new Vector2(	(m_CanvasGame.worldCamera.orthographicSize * 2.0f) / m_CanvasGUI.GetComponent<RectTransform>().rect.width,
											(m_CanvasGame.worldCamera.orthographicSize * 2.0f) / m_CanvasGUI.GetComponent<RectTransform>().rect.height);

		m_vInputPositionTransformed =	m_CameraInputsHandler.GetPanning() +
										vRatioCanvas * vScreenPosition;
		//m_vInputPositionTransformed = new Vector2(m_CanvasGame.worldCamera.transform.localPosition.x, m_CanvasGame.worldCamera.transform.localPosition.y) + vScreenPositionWithModifiers / fZoomRatio;
		m_GamePlaySelection.UpdateInputPosition(m_vInputPositionTransformed);
	}

	public override void InputsStopped()
	{
		m_GamePlaySelection.InputsStopped();
	}
}
