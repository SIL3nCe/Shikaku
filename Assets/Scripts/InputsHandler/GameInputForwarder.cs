using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInputForwarder : MonoBehaviour
{
	public GameObject m_PanelCameraInputs;

	public CameraInputsHandler m_CameraInputsHandler;
	public CanvasGameInputsHandler m_CanvasGameInputsHandler;

	public Canvas m_CanvasGUI;
	public Canvas m_CanvasGame;

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
		m_CanvasGameInputsHandler.SetCanvasGame(m_CanvasGame);
	}

    // Update is called once per frame
    void Update()
	{
		RectTransform rectTransform = GetComponent<RectTransform>();
		Vector2 vScreenPosition;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(m_CanvasGUI.transform as RectTransform, Input.mousePosition, m_CanvasGUI.worldCamera, out vScreenPosition);

		//
		// Beware, coordinate system is x+ towards left and y+ towards up
		if(		vScreenPosition.x > -rectTransform.rect.width*0.5f
			&&	vScreenPosition.x < rectTransform.rect.width * 0.5f
			&&	vScreenPosition.y > -rectTransform.rect.height * 0.5f
			&&	vScreenPosition.y < rectTransform.rect.height * 0.5f)
		{
			//
			// Camera inputs first
			float fPanelCameraInputHeight = m_PanelCameraInputs.GetComponent<RectTransform>().rect.height;
			if (vScreenPosition.y < -rectTransform.rect.height * 0.5f + fPanelCameraInputHeight)
			{
				if(null != m_currentInputHandler && m_CameraInputsHandler != m_currentInputHandler)
				{
					m_currentInputHandler.InputsStopped();
				}
				m_currentInputHandler = m_CameraInputsHandler;
			}
			else // GameCanvas inputs
			{
				if (null != m_currentInputHandler && m_CanvasGameInputsHandler != m_currentInputHandler)
				{
					m_currentInputHandler.InputsStopped();
				}
					Debug.Log("screen position before : " + vScreenPosition);
				m_currentInputHandler = m_CanvasGameInputsHandler;
				vScreenPosition.x *= 1.0f / m_CanvasGUI.GetComponent<RectTransform>().rect.width;
				vScreenPosition.y *= 1.0f / m_CanvasGUI.GetComponent<RectTransform>().rect.height;
			}
			m_currentInputHandler.HandleInputs(vScreenPosition);
		}
		else if(null != m_currentInputHandler)
		{
			m_currentInputHandler.InputsStopped();
			m_currentInputHandler = null;
		}
	}
}
