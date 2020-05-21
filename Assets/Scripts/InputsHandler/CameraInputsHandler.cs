using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraInputsHandler : InputsHandler
{
	public enum ECameraZoomType
	{
		tap,
		pinch,
	};

	public ECameraZoomType m_eZoomType;
	public float m_fDistanceStartThreshold = 0.15f;
	public float m_fPanningFactor = 1.0f;

	private bool m_bInputStarted = false;
	private Vector2 m_vLastPosition;
	private Vector2 m_vCurrentPosition;
	private GameGrid m_gameGrid;
	private Camera m_CameraGame;

	//
	// Panning
	private Vector3 m_vPosInitCamera;
	private Vector2 m_vPanningPos;
	private Vector2 m_vPanningRectSize;
	private Vector2 m_vPanningBoundsTopLeft;
	private Vector2 m_vPanningBoundsBottomRight;

	public void SetGameGrid(GameGrid gameGrid)
	{
		m_gameGrid = gameGrid;
		Vector2 vGridSize = m_gameGrid.GetSize();
		m_vPanningBoundsTopLeft = new Vector2(-vGridSize.x * 0.5f, vGridSize.y * 0.5f);
		m_vPanningBoundsBottomRight = new Vector2(vGridSize.x * 0.5f, -vGridSize.y * 0.5f);
	}

	public void SetCameraGame(Camera camera)
	{
		m_CameraGame = camera;
		m_vPosInitCamera = m_CameraGame.transform.localPosition;
		m_vPanningPos = new Vector2(0.0f, 0.0f);
		m_vPanningRectSize = new Vector2(m_CameraGame.orthographicSize, m_CameraGame.orthographicSize);
	}

	public override void HandleInputs(Vector2 vScreenPosition)
	{
		if(!m_bInputStarted)
		{
			m_bInputStarted = true;
			m_vLastPosition = new Vector2(vScreenPosition.x, vScreenPosition.y);
		}
		else
		{
			m_vLastPosition = m_vCurrentPosition;
		}
		m_vCurrentPosition = new Vector2(vScreenPosition.x, vScreenPosition.y);

		//
		// Panning or pinch zooming
		Vector2 vDelta = m_vCurrentPosition - m_vLastPosition;
		Debug.Log("vDelta: " + vDelta);
		if (vDelta.magnitude > m_fDistanceStartThreshold)
		{
			//
			// Zooming
			if (IsSecondInputTriggered())
			{

			}
			else // Panning
			{
				Vector2 vDeltaPanningPos = m_vPanningPos + vDelta * m_fPanningFactor;

				//
				// Panning horizontal
				if (vDeltaPanningPos.x > m_vPanningBoundsTopLeft.x && vDeltaPanningPos.x < m_vPanningBoundsBottomRight.x)
				{
					m_vPanningPos.x = vDeltaPanningPos.x;
				}

				//
				// Panning vertical
				if (vDeltaPanningPos.y < m_vPanningBoundsTopLeft.y && vDeltaPanningPos.y > m_vPanningBoundsBottomRight.y)
				{
					m_vPanningPos.y = vDeltaPanningPos.y;
				}

				//
				// Update position with delta
				m_CameraGame.transform.localPosition = new Vector3(
															m_vPosInitCamera.x + m_vPanningPos.x, 
															m_vPosInitCamera.y + m_vPanningPos.y, 
															m_CameraGame.transform.localPosition.z);
			}
		}
	}

	private bool IsSecondInputTriggered()
	{
		return Input.GetMouseButton(1);
	}

	public override void InputsStopped()
	{
		m_bInputStarted = false;
	}
}
