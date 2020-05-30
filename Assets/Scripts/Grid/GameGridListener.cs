using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameGridListener : MonoBehaviour
{
	public abstract void OnGameGridCreated(int iAreaCount);
	public abstract void OnGameGridStepValidated(Area area);
	public abstract void OnGameGridStepHinted(Area area);
	public abstract void OnGameGridFinished();
	public abstract void OnGameGridDestroyed();
}
