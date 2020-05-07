using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasGameInputsHandler : InputsHandler
{
	private Canvas m_CanvasGame;
	private Cell m_CellHovered;
	private Vector2 m_vInputPosition;

	public void SetCanvasGame(Canvas canvas)
	{
		m_CanvasGame = canvas;
	}

	public override void HandleInputs(Vector2 vScreenPosition)
	{
		Debug.Log("screen position middle : " + vScreenPosition);
		//vScreenPosition.x += m_CanvasGame.worldCamera.transform.localPosition.x;
		//vScreenPosition.y += m_CanvasGame.worldCamera.transform.localPosition.y;
		RectTransform rectTransform = m_CanvasGame.GetComponent<RectTransform>();
		vScreenPosition.x *= rectTransform.rect.width;
		vScreenPosition.y *= rectTransform.rect.height;
		Debug.Log("screen position after : " + vScreenPosition);
		Ray ray  = new Ray(new Vector3(vScreenPosition.x, vScreenPosition.y, m_CanvasGame.worldCamera.transform.localPosition.z), new Vector3(0.0f, 0f, 1.0f));
		//Ray ray = m_CanvasGame.worldCamera.ViewportPointToRay(new Vector3(vScreenPosition.x, vScreenPosition.y, m_CanvasGame.transform.position.z));
		RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
		Debug.DrawRay(new Vector3(ray.origin.x, ray.origin.y, m_CanvasGame.transform.position.z-3), new Vector3(0.0f, 0f, 10.0f), new Color(1.0f, 0f, 0.0f), 0.0f, true);
		if(null != hit.transform)
		{
			//
			// Update input position
			m_vInputPosition.Set(hit.point.x, hit.point.y);

			//
			// Update hovered cell

			Cell cell = null;
			if (null != hit.transform.gameObject && null != m_CellHovered && (cell = hit.transform.gameObject.GetComponent<Cell>()) != m_CellHovered)
			{
				m_CellHovered.OnMouseExit();
				m_CellHovered.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1);
			}

			//
			// Notify hovered cell
			m_CellHovered = cell;
			if(cell != null)
			{
				m_CellHovered.OnMouseEnter();
				m_CellHovered.GetComponent<SpriteRenderer>().color = new Color(1, 0, 0);
			}
		}
		else
		{
			//
			// Update input position
			m_vInputPosition.Set(float.MaxValue, float.MaxValue);

			//
			// Notify hovered cell
			if (m_CellHovered != null)
			{
				m_CellHovered.OnMouseExit();
				m_CellHovered.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1);
				m_CellHovered = null;
			}
		}
	}

	public override void InputsStopped()
	{
		if(null != m_CellHovered)
		{
			m_CellHovered.OnMouseExit();
			m_CellHovered.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1);
			m_CellHovered = null;	
		}
	}

	public Vector2 GetInputPosition()
	{
		return m_vInputPosition;
	}
}
