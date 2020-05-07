using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InputsHandler : MonoBehaviour
{
	public abstract void HandleInputs(Vector2 vScreenPosition);

	public abstract void InputsStopped();
}
