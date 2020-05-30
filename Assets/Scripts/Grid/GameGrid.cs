using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class GameGrid : MonoBehaviour
{
	//
	// GameGridListeners
	List<GameGridListener> m_aGameGridListeners;

	//
	// Cells with Height = i (y), Width = j (x)
	[Tooltip("Cell object to instantiate")]
    public GameObject m_cellPrefab;
    private float m_fCellSize = 1.0f;
    public float m_fCellSpacing = 0.08f;
    private GameObject[,] m_aGridView;
	private Vector2 m_vTopLeft;
	private int m_iWidth = 0;
	private int m_iHeight = 0;

	//
	// Model
	private GridModel m_aGridModel;

    private int m_iUsedCellCounter;
    private bool m_bGridEnded;

	// Selected area visual
    public GameObject m_CellsContainer;

    private Resolver m_resolver;

	void Start()
    {
		m_aGameGridListeners = new List<GameGridListener>();
	}

    public void Clean()
    {
        m_resolver = null;
        m_aGridModel = null;

		for (int i = 0; i < m_iHeight; ++i)
        {
            for (int j = 0; j < m_iWidth; ++j)
            {
                Destroy(m_aGridView[i, j]);
            }
        }
        m_iUsedCellCounter = 0;

		//
		// Notify listeners
		NotifyGameGridDestroyed();
	}

    public void Generate(EDifficulty eDifficulty)
    {
        Clean();

        //
        // Generator
        Debug.Log("Generate grid");
        float fTimeCounter = Time.realtimeSinceStartup;
        GridGenerator generator = new GridGenerator();
        var retVal = generator.Generate(eDifficulty);
        Assert.IsTrue(retVal.Item1);
        Debug.Log("Generated grid in " + (Time.realtimeSinceStartup - fTimeCounter));

        m_aGridModel = retVal.Item2;
        m_aGridModel.m_aAreaList.Sort();

        m_iHeight = m_aGridModel.m_iHeight;
        m_iWidth = m_aGridModel.m_iWidth;
        
        //
        // Solver
        Debug.Log("Resolving grid");
        fTimeCounter = Time.realtimeSinceStartup;
        m_resolver = new Resolver();
        m_resolver.Resolve(m_aGridModel);
        Debug.Log("Resolved grid in " + (Time.realtimeSinceStartup - fTimeCounter));

        //
        // Game grid
        m_aGridView = new GameObject[m_iHeight, m_iWidth];

		// Set grid center in 0,0
		float fCellSize = m_fCellSize;
		float fHalfCellSize = fCellSize * 0.5f;
		float fCellSpacing = m_fCellSpacing;
		float fHalfCellSpacing = fCellSpacing * 0.5f;
		if (m_iWidth%2 != 0)
		{
			m_vTopLeft.x = -fHalfCellSize - (m_iWidth/2) * (fCellSpacing + fCellSize);
		}
		else
		{
			m_vTopLeft.x = - fHalfCellSpacing - (m_iWidth/2) * fCellSize - (m_iWidth/2 - 1) * fCellSpacing;
		}

		if (m_iHeight%2 != 0)
		{
			m_vTopLeft.y = fHalfCellSize + (m_iHeight/2) * (fCellSpacing + fCellSize);
		}
		else
		{
			m_vTopLeft.y =  fHalfCellSpacing + (m_iHeight/2) * fCellSize + (m_iWidth/2 - 1) * fCellSpacing;
		}

        float x = m_vTopLeft.x, y = m_vTopLeft.y;
        for (int iHeight = 0; iHeight < m_iHeight; ++iHeight)
        {
            for (int iWidth = 0; iWidth < m_iWidth; ++iWidth)
            {
                m_aGridView[iHeight, iWidth] = Instantiate(m_cellPrefab, m_CellsContainer.transform);
				m_aGridView[iHeight, iWidth].GetComponent<RectTransform>().anchoredPosition3D = new Vector3(x + fHalfCellSize, y - fHalfCellSize, 0.0f);
                m_aGridView[iHeight, iWidth].GetComponent<Cell>().Initialize(iHeight, iWidth, fCellSize, this);
				x += fCellSize + fCellSpacing;
				m_iUsedCellCounter++;
            }
            x = m_vTopLeft.x;
            y -= (fCellSize + fCellSpacing);
        }

        int nArea = m_aGridModel.m_aAreaList.Count;
        for (int i = 0; i < nArea; ++i)
        {
            Area area = m_aGridModel.m_aAreaList[i];
            area.Reset();
            m_aGridView[area.x, area.y].GetComponent<Cell>().SetAreaSize(area.value);
        }

        m_bGridEnded = false;

		//
		// Notify listeners
		NotifyGameGridCreated(nArea);
	}

    public bool CheckGridFeasibility()
    {
        if (m_bGridEnded)
            return true;

        // Check in solutions if current grid is valid based on solver solutions
        return -1 != m_resolver.CheckGridFeasbility(m_aGridModel);
    }

    public bool TakeAGridStep()
    {
        if (m_bGridEnded)
            return false;

        // Search for the first not done yet origin and try to complete it
        int solutionId = m_resolver.CheckGridFeasbility(m_aGridModel);
        if (-1 != solutionId)
        {
            int nArea = m_aGridModel.m_aAreaList.Count;
            for (int iArea = 0; iArea < nArea; ++iArea)
            {
                if (!m_aGridModel.m_aAreaList[iArea].IsCompleted())
                {
                    Area area = m_resolver.GetCompletedArea(solutionId, iArea);

					//
					// Notify listeners
					NotifyGameGridStepHinted(area);

                    return true;
                }
            }
        }
        
        return false;
    }

    private void OnGridEnded()
    {
        // Grid is ended
        m_bGridEnded = true;
        Debug.Log("GRID ENDED, GGWP");

		//
		// Notify listeners
		NotifyGameGridFinished();
    }

	public void SelectArea(Cell cellTopLeft, int iAreaWidth, int iAreaHeight, int iAreaId)
	{
		Area area = m_aGridModel.m_aAreaList[iAreaId];
		area.startX = cellTopLeft.GetCoordinates().x;
		area.startY = cellTopLeft.GetCoordinates().y;
		area.width = iAreaWidth;
		area.height = iAreaHeight;

		//
		// Check for end of grid
		m_iUsedCellCounter -= iAreaWidth * iAreaHeight;
		if (m_iUsedCellCounter == 0)
		{
			OnGridEnded();
		}
	}

    public bool IsAreaValid(Vector2Int vTopLeft, int areaWidth, int areaHeight, ref int AreaId)
    {
        int nOrigin = 0;

        int nCells = areaWidth * areaHeight;

        for (int i = 0; i < areaHeight; ++i)
        {
            int cellX = vTopLeft.x + i;
            for (int j = 0; j < areaWidth; ++j)
            {
                int cellY = vTopLeft.y + j;

                Cell cell = m_aGridView[cellX, cellY].GetComponent<Cell>();

                // A cell in given area already belong to an area
                if (cell.IsInArea())
                    return false;

                if (cell.IsAreaOrigin())
                {
                    // More than one origin cell in area
                    if (++nOrigin != 1)
                        return false;

                    // Too much/not enough cells in area based on origin cell value
                    if (cell.GetAreaOriginValue() != nCells)
                        return false;

                    AreaId = m_aGridModel.GetAreaId(cellX, cellY);
                }
            }
        }

        return nOrigin != 0;
    }

	public Vector2 GetTopLeft()
	{
		return m_vTopLeft;
	}

	public Vector2 GetSize()
	{
		float fFactor = (m_fCellSize + m_fCellSpacing);
		return new Vector2(m_iWidth * fFactor - m_fCellSpacing, m_iHeight * fFactor - m_fCellSpacing);
	}

	public GameObject[,] GetGridView()
	{
		return m_aGridView;
	}

	public float GetCellSize()
	{
		return m_fCellSize;
	}

	public float GetCellSpacing()
	{
		return m_fCellSpacing;
	}

	public bool IsGridCompleted()
	{
		return m_bGridEnded;
	}

	public int GetWidth()
	{
		return m_iWidth;
	}

	public int GetHeight()
	{
		return m_iHeight;
	}

	private void NotifyGameGridCreated(int iAreasCount)
	{
		foreach(GameGridListener listener in m_aGameGridListeners)
		{
			listener.OnGameGridCreated(iAreasCount);
		}
	}

	private void NotifyGameGridStepHinted(Area area)
	{
		foreach (GameGridListener listener in m_aGameGridListeners)
		{
			listener.OnGameGridStepHinted(area);
		}
	}

	private void NotifyGameGridStepValidated(Area area)
	{
		foreach (GameGridListener listener in m_aGameGridListeners)
		{
			listener.OnGameGridStepValidated(area);
		}
	}

	private void NotifyGameGridFinished()
	{
		foreach (GameGridListener listener in m_aGameGridListeners)
		{
			listener.OnGameGridFinished();
		}
	}

	private void NotifyGameGridDestroyed()
	{
		foreach (GameGridListener listener in m_aGameGridListeners)
		{
			listener.OnGameGridDestroyed();
		}
	}

	public void DeleteArea(int iAreaId)
	{
		for (int iRow = 0; iRow < m_iHeight; ++iRow)
		{
			for (int iColumn = 0; iColumn < m_iWidth; ++iColumn)
			{
				Cell cell = m_aGridView[iRow, iColumn].GetComponent<Cell>();
				if (cell.IsInGivenArea(iAreaId))
				{
					Area area = m_aGridModel.m_aAreaList[cell.areaId];

					area.Reset();
					cell.areaId = -1;
					cell.GetComponent<SpriteRenderer>().color = Color.white;
					m_iUsedCellCounter++;
				}
			}
		}
	}
}
