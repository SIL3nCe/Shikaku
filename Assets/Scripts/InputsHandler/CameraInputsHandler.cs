﻿using UnityEngine;

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
	public float m_fPanningBoundsEdgeSpace = 1.0f;

	//
	// Debug
	public bool m_bDebugDrawPanning = false;

	private bool m_bInputStarted = false;
	private Vector2 m_vStartPosition;
	private Vector2 m_vLastPosition;
	private Vector2 m_vCurrentPosition;
	private GameGrid m_gameGrid;
	private Camera m_CameraGame;
	private Canvas m_CanvasGUI;
	private GameObject m_CanvasGUIPanelCameraInputs;
	private GameObject m_CanvasGUIPanelGUI;

	//
	// Panning
	private bool m_bPanningPossible;
	private Vector3 m_vPosInitCamera = new Vector2(0.0f, 0.0f);
	private Vector3 m_vPosInitCameraWorld = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningPosInit = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningPos = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningRectSize = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningBoundsTopLeft = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningBoundsBottomRight = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningOffsetTopLeft = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningOffsetBottomRight = new Vector2(0.0f, 0.0f);

	//
	// Zooming
	private float m_fZoomRatio = 1.0f;
	public float m_fZoomMaxRatio = 3.0f;

	//
	// Pinch Zooming
	private bool m_bZoomPinchStarted = false;
	private float m_fZoomPinchInitialDistance;
	private float m_fZoomPinchRatioInit;
	public float m_fZoomPinchRatio = 0.5f;

	//
	// Tap Zooming
	public int m_iZoomTapLevels = 3;
	private int m_iZoomTapCurrentLevel = 0;
	private float m_fZoomTapRatioStep;
	public float m_fZoomTapDoubleInputTimeThreshold = 0.5f;
	private bool m_bZoomTapFirstInput = false;
	private bool m_bZoomTapSecondInput = false;
	private float m_fZoomTapFirstInputTimestamp;

	private void UpdatePanningPossibility()
	{
		m_bPanningPossible =		m_vPanningBoundsTopLeft.x < m_vPanningPosInit.x - m_vPanningRectSize.x * 0.5f
								&&	m_vPanningBoundsTopLeft.y > m_vPanningPosInit.y + m_vPanningRectSize.y * 0.5f
								&&	m_vPanningBoundsBottomRight.x > m_vPanningPosInit.x + m_vPanningRectSize.x * 0.5f
								&&	m_vPanningBoundsBottomRight.y < m_vPanningPosInit.y - m_vPanningRectSize.y * 0.5f;
	}

	public void SetGameGrid(GameGrid gameGrid)
	{
		m_gameGrid = gameGrid;

		//
		// Compute panning bounds - part 1
		Vector2 vGridSize = m_gameGrid.GetSize();
		m_vPanningBoundsTopLeft += new Vector2(-vGridSize.y * 0.5f, vGridSize.x * 0.5f);
		m_vPanningBoundsBottomRight += new Vector2(vGridSize.y * 0.5f, -vGridSize.x * 0.5f);
		m_vPanningBoundsTopLeft += new Vector2(-m_fPanningBoundsEdgeSpace, m_fPanningBoundsEdgeSpace);
		m_vPanningBoundsBottomRight += new Vector2(m_fPanningBoundsEdgeSpace, -m_fPanningBoundsEdgeSpace);

		//
		// Update panning possibility
		UpdatePanningPossibility();
	}

	public void SetCameraGame(Camera camera)
	{
		m_CameraGame = camera;
		m_vPosInitCamera = m_CameraGame.transform.localPosition;
		m_vPosInitCameraWorld = m_CameraGame.transform.position;

		//
		// Compute panning bounds - part 2
		SetPanningRectSize(new Vector2(m_CameraGame.orthographicSize, m_CameraGame.orthographicSize));
	}

	public void SetCanvasGUI(Canvas canvas)
	{
		m_CanvasGUI = canvas;
	}

	public void SetCanvasGUIPanelCameraInputs(GameObject panelCameraInputs)
	{
		m_CanvasGUIPanelCameraInputs = panelCameraInputs;

		//
		// Compute panning bounds - part 3
		m_vPanningOffsetBottomRight.y = -m_CanvasGUIPanelCameraInputs.GetComponent<RectTransform>().rect.height;
		m_vPanningBoundsBottomRight += m_vPanningOffsetBottomRight;

		//
		// Initial panning
		m_vPanningPosInit.y	+= m_vPanningOffsetBottomRight.y * 0.5f;
		m_vPanningPos		= m_vPanningPosInit;
		ApplyPanningtoCamera();

		UpdatePanningPossibility();
	}

	public void SetCanvasGUIPanelGUI(GameObject panelGUI)
	{
		m_CanvasGUIPanelGUI = panelGUI;

		//
		// Compute panning bounds - part 4
		m_vPanningOffsetTopLeft.y = m_CanvasGUIPanelGUI.GetComponent<RectTransform>().rect.height;
		m_vPanningBoundsTopLeft += m_vPanningOffsetTopLeft;

		//
		// Initial panning
		m_vPanningPosInit.y += m_vPanningOffsetTopLeft.y * 0.5f;
		m_vPanningPos = m_vPanningPosInit;
		ApplyPanningtoCamera();

		UpdatePanningPossibility();
	}

	private void SetPanningRectSize(Vector2 vRectSize)
	{
		if(!Mathf.Approximately(m_vPanningRectSize.x, vRectSize.x) || !Mathf.Approximately(m_vPanningRectSize.y, vRectSize.y))
		{
			m_vPanningBoundsTopLeft.x -= m_vPanningRectSize.x;
			m_vPanningBoundsTopLeft.y -= -m_vPanningRectSize.y;
			m_vPanningBoundsBottomRight.x -= -m_vPanningRectSize.x;
			m_vPanningBoundsBottomRight.y -= m_vPanningRectSize.y;

			m_vPanningRectSize = vRectSize;
			m_CameraGame.orthographicSize = vRectSize.x;

			m_vPanningBoundsTopLeft.x += m_vPanningRectSize.x;
			m_vPanningBoundsTopLeft.y += -m_vPanningRectSize.y;
			m_vPanningBoundsBottomRight.x += -m_vPanningRectSize.x;
			m_vPanningBoundsBottomRight.y += m_vPanningRectSize.y;

			//
			// Update panning possibility
			UpdatePanningPossibility();
		}
	}

	private void ApplyPanningtoCamera()
	{
		m_CameraGame.transform.localPosition = new Vector3(
													m_vPosInitCamera.x + m_vPanningPos.x,
													m_vPosInitCamera.y + m_vPanningPos.y,
													m_CameraGame.transform.localPosition.z);
	}

	private void Start()
	{
		//
		// Create tap-zoom step ratio
		m_fZoomTapRatioStep = m_fZoomMaxRatio / m_iZoomTapLevels;
	}

	private void Update()
	{
		//
		// Debug
		if(m_bDebugDrawPanning)
		{
			Debug.DrawLine(new Vector3(m_vPosInitCameraWorld.x + m_vPanningBoundsTopLeft.x, m_vPosInitCameraWorld.y + m_vPanningBoundsTopLeft.y, 20.0f), new Vector3(m_vPosInitCameraWorld.x + m_vPanningBoundsBottomRight.x, m_vPosInitCameraWorld.y + m_vPanningBoundsTopLeft.y, 20.0f), new Color(1, 0, 0));
			Debug.DrawLine(new Vector3(m_vPosInitCameraWorld.x + m_vPanningBoundsTopLeft.x, m_vPosInitCameraWorld.y + m_vPanningBoundsTopLeft.y, 20.0f), new Vector3(m_vPosInitCameraWorld.x + m_vPanningBoundsTopLeft.x, m_vPosInitCameraWorld.y + m_vPanningBoundsBottomRight.y, 20.0f), new Color(1, 0, 0));
			Debug.DrawLine(new Vector3(m_vPosInitCameraWorld.x + m_vPanningBoundsBottomRight.x, m_vPosInitCameraWorld.y + m_vPanningBoundsBottomRight.y, 20.0f), new Vector3(m_vPosInitCameraWorld.x + m_vPanningBoundsBottomRight.x, m_vPosInitCameraWorld.y + m_vPanningBoundsTopLeft.y, 20.0f), new Color(1, 0, 0));
			Debug.DrawLine(new Vector3(m_vPosInitCameraWorld.x + m_vPanningBoundsBottomRight.x, m_vPosInitCameraWorld.y + m_vPanningBoundsBottomRight.y, 20.0f), new Vector3(m_vPosInitCameraWorld.x + m_vPanningBoundsTopLeft.x, m_vPosInitCameraWorld.y + m_vPanningBoundsBottomRight.y, 20.0f), new Color(1, 0, 0));

			Debug.DrawLine(new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x - 0.5f, m_vPosInitCameraWorld.y + m_vPanningPos.y, 20.0f), new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x + 0.5f, m_vPosInitCameraWorld.y + m_vPanningPos.y, 20.0f), new Color(1.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x, m_vPosInitCameraWorld.y + m_vPanningPos.y + 0.5f, 20.0f), new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x, m_vPosInitCameraWorld.y + m_vPanningPos.y - 0.5f, 20.0f), new Color(1.0f, 0.0f, 0.0f));

			Debug.DrawLine(new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x - m_vPanningRectSize.x, m_vPosInitCameraWorld.y + m_vPanningPos.y + m_vPanningRectSize.y, 20.0f), new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x + m_vPanningRectSize.x, m_vPosInitCameraWorld.y + m_vPanningPos.y + m_vPanningRectSize.y, 20.0f), new Color(0.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x - m_vPanningRectSize.x, m_vPosInitCameraWorld.y + m_vPanningPos.y + m_vPanningRectSize.y, 20.0f), new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x - m_vPanningRectSize.x, m_vPosInitCameraWorld.y + m_vPanningPos.y - m_vPanningRectSize.y, 20.0f), new Color(0.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x + m_vPanningRectSize.x, m_vPosInitCameraWorld.y + m_vPanningPos.y - m_vPanningRectSize.y, 20.0f), new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x - m_vPanningRectSize.x, m_vPosInitCameraWorld.y + m_vPanningPos.y - m_vPanningRectSize.y, 20.0f), new Color(0.0f, 0.0f, 0.0f));
			Debug.DrawLine(new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x + m_vPanningRectSize.x, m_vPosInitCameraWorld.y + m_vPanningPos.y - m_vPanningRectSize.y, 20.0f), new Vector3(m_vPosInitCameraWorld.x + m_vPanningPos.x + m_vPanningRectSize.x, m_vPosInitCameraWorld.y + m_vPanningPos.y + m_vPanningRectSize.y, 20.0f), new Color(0.0f, 0.0f, 0.0f));
		}

		//
		// Tap-zooming
		if(m_bZoomTapFirstInput && !m_bZoomTapSecondInput && m_fZoomTapDoubleInputTimeThreshold < Time.time - m_fZoomTapFirstInputTimestamp)
		{
			//
			// Zoom
			SetZoomLevel(m_iZoomTapCurrentLevel + 1);

			m_bZoomTapFirstInput = false;
			m_fZoomTapFirstInputTimestamp = 0.0f;
		}
	}

	private void SetZoomLevel(int iNewZoomLevel)
	{
		if(m_iZoomTapCurrentLevel != iNewZoomLevel)
		{
			int iDeltaZoomLevel = m_iZoomTapCurrentLevel - iNewZoomLevel;
			m_iZoomTapCurrentLevel = iNewZoomLevel;

			//
			// Update ratio
			SetZoomRatio(Mathf.Max(1.0f, Mathf.Min(m_fZoomMaxRatio, m_fZoomRatio - iDeltaZoomLevel*m_fZoomTapRatioStep)));
		}
	}

	private void SetZoomRatio(float fNewZoomRatio)
	{
		if(!Mathf.Approximately(m_fZoomRatio, fNewZoomRatio))
		{
			//
			// Remove bottom panning with former ratio
			bool bUpdatePanningTop		= Mathf.Approximately(m_vPanningPos.y, m_vPanningBoundsTopLeft.y);
			bool bUpdatePanningLeft		= Mathf.Approximately(m_vPanningPos.x, m_vPanningBoundsTopLeft.x);
			bool bUpdatePanningBottom	= Mathf.Approximately(m_vPanningPos.y, m_vPanningBoundsBottomRight.y);
			bool bUpdatePanningRight	= Mathf.Approximately(m_vPanningPos.x, m_vPanningBoundsBottomRight.x);

			//
			// Update ratio
			float fDeltaZoomRatio = m_fZoomRatio - fNewZoomRatio;
			m_fZoomRatio = fNewZoomRatio;

			//
			// Update camera and panning rect size
			m_vPanningBoundsBottomRight.y -= m_vPanningOffsetBottomRight.y;
			m_vPanningBoundsBottomRight.y -= m_vPanningOffsetTopLeft.y;
			float fNewOrthographicSize = m_CameraGame.orthographicSize + fDeltaZoomRatio;
			float fPanningOffsetRatioYBottomRight = m_vPanningOffsetBottomRight.y * fNewOrthographicSize / m_CameraGame.orthographicSize; // proportions because canvas initial size = camera initial orthographic size
			float fPanningOffsetRatioYTopLeft = m_vPanningOffsetTopLeft.y * fNewOrthographicSize / m_CameraGame.orthographicSize; // proportions because canvas initial size = camera initial orthographic size
			SetPanningRectSize(new Vector2(fNewOrthographicSize, fNewOrthographicSize));
			m_vPanningOffsetBottomRight.y = fPanningOffsetRatioYBottomRight;
			m_vPanningOffsetTopLeft.y = fPanningOffsetRatioYTopLeft;
			m_vPanningBoundsBottomRight.y += m_vPanningOffsetBottomRight.y;
			m_vPanningBoundsTopLeft.y += m_vPanningOffsetTopLeft.y;

			//
			// Clamp panning position to bottom right if it is already in max panning position
			if (bUpdatePanningTop || bUpdatePanningRight || bUpdatePanningBottom || bUpdatePanningLeft)
			{
				//
				// Horizontal
				if		(bUpdatePanningLeft)	{	m_vPanningPos.x = m_vPanningBoundsTopLeft.x;		}
				else if (bUpdatePanningRight)	{	m_vPanningPos.x = m_vPanningBoundsBottomRight.x;	}

				//
				// Vertical
				if		(bUpdatePanningTop)		{	m_vPanningPos.y = m_vPanningBoundsTopLeft.y;		}
				else if (bUpdatePanningBottom)	{	m_vPanningPos.y = m_vPanningBoundsBottomRight.y;	}

				ApplyPanningtoCamera();
			}
		}
	}

	public override void HandleInputs(Vector2 vScreenPosition)
	{
		m_vLastPosition = new Vector2(m_vCurrentPosition.x, m_vCurrentPosition.y);
		m_vCurrentPosition = new Vector2(vScreenPosition.x, vScreenPosition.y);

		if(!m_bInputStarted)
		{
			m_bInputStarted = true;
			m_vStartPosition = new Vector2(vScreenPosition.x, vScreenPosition.y);
			m_vCurrentPosition = new Vector2(m_vStartPosition.x, m_vStartPosition.y);
			m_vLastPosition = new Vector2(m_vCurrentPosition.x, m_vCurrentPosition.y);
		}

		//
		// Panning or pinch-zooming
		Vector2 vDelta = m_vCurrentPosition - m_vLastPosition;
		if (vDelta.magnitude > m_fDistanceStartThreshold)
		{
			//
			// Pinch-zooming
			Vector2 vScreenPositionSecondInput = new Vector2();
			if (IsSecondInputTriggered(ref vScreenPositionSecondInput))
			{
				RectTransform rectTransform = m_CanvasGUI.GetComponent<RectTransform>();
				RectTransformUtility.ScreenPointToLocalPointInRectangle(m_CanvasGUI.transform as RectTransform, vScreenPositionSecondInput, m_CanvasGUI.worldCamera, out vScreenPositionSecondInput);

				if (!m_bZoomPinchStarted)
				{
					m_bZoomPinchStarted = true;
					m_fZoomPinchRatioInit = m_fZoomRatio;
					m_fZoomPinchInitialDistance = Vector3.Distance(m_vStartPosition, vScreenPositionSecondInput);
				}

				//
				// Proportionnality between start state and current one
				float fPinchDistance = Vector3.Distance(m_vStartPosition, vScreenPositionSecondInput);
				float fZoomRatioNew = m_fZoomPinchRatioInit * fPinchDistance / m_fZoomPinchInitialDistance;
				float fZoomRatioFactored = m_fZoomPinchRatio * fZoomRatioNew;
				float fZoomRatioFinal = Mathf.Max(1.0f, Mathf.Min(m_fZoomMaxRatio, fZoomRatioFactored));
				SetZoomRatio(fZoomRatioFinal);
			}
			else if(!m_bZoomPinchStarted && m_bPanningPossible) // Panning
			{
				Vector2 vDeltaPanningPos = m_vPanningPos + vDelta * m_fPanningFactor;

				//
				// Panning horizontal
				if (vDelta.x < 0)
				{
					m_vPanningPos.x = Mathf.Max(m_vPanningBoundsTopLeft.x, vDeltaPanningPos.x);
				}
				else if (vDelta.x > 0)
				{
					m_vPanningPos.x = Mathf.Min(m_vPanningBoundsBottomRight.x, vDeltaPanningPos.x);
				}

				//
				// Panning vertical
				if (vDelta.y > 0)
				{
					m_vPanningPos.y = Mathf.Min(m_vPanningBoundsTopLeft.y, vDeltaPanningPos.y);
				}
				else if (vDelta.y < 0)
				{
					m_vPanningPos.y = Mathf.Max(m_vPanningBoundsBottomRight.y, vDeltaPanningPos.y);
				}

				//
				// Update position with delta
				ApplyPanningtoCamera();
			}
		}
	}

	private bool IsSecondInputTriggered(ref Vector2 vScreenPosition)
	{
#if UNITY_STANDALONE
		vScreenPosition = Input.mousePosition;
		return Input.GetMouseButton(1);
#elif UNITY_ANDROID
		if(Input.touchCount > 0)
		{
			for(int iTouchIndex = 0; iTouchIndex < Input.touchCount; ++iTouchIndex)
			{
				Touch touch = Input.GetTouch(iTouchIndex);
				if (1 == touch.fingerId)
				{
					vScreenPosition = touch.position;
					return true;
				}
			}
		}
		return false;
#endif // UNITY_STANDALONE
	}

	public override void InputsStopped()
	{
		m_bInputStarted = false;
		
		//
		// Pinch-zooming
		m_bZoomPinchStarted = false;

		//
		// Tap-zooming
		if (Vector3.Distance(m_vStartPosition, m_vLastPosition) < m_fDistanceStartThreshold)
		{
			if (!m_bZoomTapFirstInput)
			{
				m_bZoomTapFirstInput = true;
				m_fZoomTapFirstInputTimestamp = Time.time;
			}
			else if(m_bZoomTapFirstInput)
			{
				if(m_fZoomTapDoubleInputTimeThreshold > Time.time - m_fZoomTapFirstInputTimestamp)
				{
					//
					// Unzoom if double tap is contained in time threshold
					SetZoomLevel(m_iZoomTapCurrentLevel - 1);
				}

				m_bZoomTapFirstInput = false;
				m_bZoomTapSecondInput = false;
			}
		}
		else
		{
			m_bZoomTapFirstInput = false;
			m_bZoomTapSecondInput = false;
		}
	}

	//
	// Getters
	public Vector2 GetPanning()
	{
		return m_vPanningPos;
	}

	public float GetZoomRatio()
	{
		return m_fZoomRatio;
	}

	public float GetZoomRatioMax()
	{
		return m_fZoomMaxRatio;
	}
}
