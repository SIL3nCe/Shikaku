﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameInputForwarder : MonoBehaviour
{
	public CameraInputsHandler m_CameraInputsHandler;
	public CanvasGameInputsHandler m_CanvasGameInputsHandler;
	public GameGUIInputsHandler m_GameGUIInputsHandler;

	public Canvas m_CanvasGUI;
	public GameObject m_PanelGameGUI;
	public GameObject m_PanelCameraInputs;
	public GameObject m_PanelGUI;
	public RawImage m_ImageCameraGame;
	public Canvas m_CanvasGame;

	public Camera m_CameraGame;
	public GameGrid m_GameGrid;

	public GamePlaySelection m_GamePlaySelection;

	private InputsHandler m_currentInputHandler;

    // Start is called before the first frame update
    void Start()
    {
		m_currentInputHandler = null;

		Debug.Assert(null != m_PanelCameraInputs);
		Debug.Assert(null != m_CameraInputsHandler);
		Debug.Assert(null != m_CanvasGameInputsHandler);
		Debug.Assert(null != m_CanvasGUI);
		Debug.Assert(null != m_CanvasGame);

		//
		// Setup input handlers
		m_CanvasGameInputsHandler.SetGamePlaySelection(m_GamePlaySelection);
		m_CanvasGameInputsHandler.SetCanvasGame(m_CanvasGame);
		m_CanvasGameInputsHandler.SetCanvasGUI(m_CanvasGUI);
		m_CanvasGameInputsHandler.SetCameraInputsHandler(m_CameraInputsHandler);
		m_CanvasGameInputsHandler.SetImageCameraGame(m_ImageCameraGame);
		m_CameraInputsHandler.SetCameraGame(m_CameraGame);
		m_CameraInputsHandler.SetCanvasGUI(m_CanvasGUI);
		m_CameraInputsHandler.SetGameGrid(m_GameGrid);
		m_CameraInputsHandler.SetCanvasGUIPanelCameraInputs(m_PanelCameraInputs);
		m_CameraInputsHandler.SetCanvasGUIPanelGUI(m_PanelGUI);
	}

	bool IsInputTriggered(ref Vector2 vScreenPosition)
	{
#if UNITY_STANDALONE
		vScreenPosition = Input.mousePosition;
		return Input.GetMouseButton(0);
#elif UNITY_ANDROID
		if(Input.touchCount > 0)
		{
			for(int iTouchIndex = 0; iTouchIndex < Input.touchCount; ++iTouchIndex)
			{
				Touch touch = Input.GetTouch(iTouchIndex);
				if (0 == touch.fingerId)
				{
					vScreenPosition = touch.position;
					return true;
				}
			}
		}
		return false;
#endif // UNITY_STANDALONE
	}

	// Update is called once per frame
	void Update()
	{
		Vector2 vScreenPosition = new Vector2();
		if (IsInputTriggered(ref vScreenPosition))
		{
			RectTransform rectTransform = m_CanvasGUI.GetComponent<RectTransform>();
			RectTransformUtility.ScreenPointToLocalPointInRectangle(m_CanvasGUI.transform as RectTransform, vScreenPosition, m_CanvasGUI.worldCamera, out vScreenPosition);

			//
			// Beware, coordinate system is x+ towards left and y+ towards up
			if (null == m_currentInputHandler)
			{
				if (	vScreenPosition.x > -rectTransform.rect.width * 0.5f
					&&	vScreenPosition.x < rectTransform.rect.width * 0.5f
					&&	vScreenPosition.y > -rectTransform.rect.height * 0.5f
					&&	vScreenPosition.y < rectTransform.rect.height * 0.5f)
				{
					//
					// Retrieve height of different GUI components
					float fPanelGameGUIHeight = m_PanelGameGUI.GetComponent<RectTransform>().rect.height;
					float fPanelCameraInputHeight = m_PanelCameraInputs.GetComponent<RectTransform>().rect.height;

					//
					// GameGUI first
					if (vScreenPosition.y < -rectTransform.rect.height * 0.5f + fPanelGameGUIHeight)
					{
						m_currentInputHandler = m_GameGUIInputsHandler;
					}
					// Camera inputs 
					else if (vScreenPosition.y < -rectTransform.rect.height * 0.5f + fPanelCameraInputHeight)
					{
						m_currentInputHandler = m_CameraInputsHandler;
					}
					else // GameCanvas inputs
					{
						m_currentInputHandler = m_CanvasGameInputsHandler;
					}
				}
			}

			if (null != m_currentInputHandler)
			{
				m_currentInputHandler.HandleInputs(vScreenPosition);
			}
		}
		else if (null != m_currentInputHandler)
		{
			m_currentInputHandler.InputsStopped();
			m_currentInputHandler = null;
		}
	}
}
