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
	private GameObject m_CanvasGUIPanelCameraInputs;

	//
	// Panning
	private bool m_bPanningPossible;
	private Vector3 m_vPosInitCamera = new Vector2(0.0f, 0.0f);
	private Vector3 m_vPosInitCameraWorld = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningPos = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningRectSize = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningBoundsTopLeft = new Vector2(0.0f, 0.0f);
	private Vector2 m_vPanningBoundsBottomRight = new Vector2(0.0f, 0.0f);

	//
	// Zooming
	private float m_fZoomRatio = 1.0f;
	public float m_fZoomMaxRatio = 3.0f;
	public int m_iZoomTapLevels = 3;
	private int m_iZoomTapCurrentLevel = 0;
	private float m_fZoomTapRatioStep;
	private float m_fZoomTapDoubleInputTimeThreshold = 0.5f;
	private bool m_bZoomTapFirstInput = false;
	private bool m_bZoomTapSecondInput = false;
	private float m_fZoomTapFirstInputTimestamp;

	private void UpdatePanningPossibility()
	{
		m_bPanningPossible =		m_vPanningBoundsTopLeft.x < m_vPanningPos.x - m_vPanningRectSize.x * 0.5f
								&&	m_vPanningBoundsTopLeft.y > m_vPanningPos.y + m_vPanningRectSize.y * 0.5f
								&&	m_vPanningBoundsBottomRight.x > m_vPanningPos.x + m_vPanningRectSize.x * 0.5f
								&&	m_vPanningBoundsBottomRight.y < m_vPanningPos.y - m_vPanningRectSize.y * 0.5f;
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
		m_vPanningPos = new Vector2(0.0f, 0.0f);
		SetPanningRectSize(new Vector2(m_CameraGame.orthographicSize, m_CameraGame.orthographicSize));
	}

	public void SetCanvasGUIPanelCameraInputs(GameObject panelCameraInputs)
	{
		m_CanvasGUIPanelCameraInputs = panelCameraInputs;

		//
		// Compute panning bounds - part 3
		float fPanelOffset = -m_CanvasGUIPanelCameraInputs.GetComponent<RectTransform>().rect.height;
		m_vPanningBoundsBottomRight.y += fPanelOffset;

		//
		// Initial panning
		m_vPanningPos.y += fPanelOffset * 0.5f;
		m_CameraGame.transform.localPosition = new Vector3(
													m_vPosInitCamera.x + m_vPanningPos.x,
													m_vPosInitCamera.y + m_vPanningPos.y,
													m_CameraGame.transform.localPosition.z);

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

			m_vPanningBoundsTopLeft.x += m_vPanningRectSize.x;
			m_vPanningBoundsTopLeft.y += -m_vPanningRectSize.y;
			m_vPanningBoundsBottomRight.x += -m_vPanningRectSize.x;
			m_vPanningBoundsBottomRight.y += m_vPanningRectSize.y;

			//
			// Update panning possibility
			UpdatePanningPossibility();
		}
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
			float fDeltaZoomRatio = m_fZoomRatio - fNewZoomRatio;
			m_fZoomRatio = fNewZoomRatio;

			//
			// Update camera and panning rect size
			m_CameraGame.orthographicSize += fDeltaZoomRatio;
			SetPanningRectSize(new Vector2(m_CameraGame.orthographicSize, m_CameraGame.orthographicSize));
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

			//
			// Tap-zooming
			if (!m_bZoomTapFirstInput)
			{
				m_bZoomTapFirstInput = true;
				m_fZoomTapFirstInputTimestamp = Time.time;
			}
			else
			{
				m_bZoomTapSecondInput = true;
			}
		}
		else
		{
			//
			// Tap-zooming
			if(Vector2.Distance(m_vCurrentPosition, m_vLastPosition) >= m_fDistanceStartThreshold)
			{
				m_bZoomTapFirstInput = false;
				m_bZoomTapSecondInput = false;
			}
		}

		//
		// Panning or pinch-zooming
		Vector2 vDelta = m_vCurrentPosition - m_vLastPosition;
		if (vDelta.magnitude > m_fDistanceStartThreshold)
		{
			//
			// Zooming
			if (IsSecondInputTriggered())
			{

			}
			else if(m_bPanningPossible) // Panning
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

	public override void InputsStopped(Vector2 vScreenPosition)
	{
		m_bInputStarted = false;

		//
		// Tap-zooming 
		if(m_bZoomTapFirstInput && !m_bZoomTapSecondInput)
		{
			m_fZoomTapFirstInputTimestamp = Time.time;
		}
		else if(m_bZoomTapFirstInput && m_bZoomTapSecondInput && m_fZoomTapDoubleInputTimeThreshold > Time.time - m_fZoomTapFirstInputTimestamp)
		{
			//
			// Unzoom if double tap is contained in time threshold
			SetZoomLevel(m_iZoomTapCurrentLevel - 1);

			m_bZoomTapFirstInput = false;
			m_bZoomTapSecondInput = false;
		}
	}
}
