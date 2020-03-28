using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System;

class GridGenerator
{
	public enum EDifficulty
	{
		easy,
		medium,
		hard,
	}

	private enum EStartingPointCategory
	{
		small,
		medium,
		large,
	}

	public static int AXIS_Y = 0;
	public static int AXIS_X = 1;

	private class ShikakuBlock
	{
		public ShikakuBlock()
		{
			pos = new int[] { 0,0 };
			size = new int[] { 0, 0 };
			iAreaValue = 0;
		}

		public ShikakuBlock(int[] sPos, int[] sSize)
		{
			pos = sPos;
			size = sSize;
			iAreaValue = sSize[AXIS_X] * sSize[AXIS_Y];
		}

		public override string ToString()
		{
			return "block : pos("+pos[AXIS_Y]+","+ pos[AXIS_X] + "), size("+size[AXIS_Y]+","+size[AXIS_X]+ ") = "+iAreaValue;
		}

		public int[] pos;
		public int[] size;
		public int iAreaValue;
	}

	private const int m_iDirectionTop = -1;
	private const int m_iDirectionRight = +1;
	private const int m_iDirectionBottom = +1;
	private const int m_iDirectionLeft = -1;

	private List<ShikakuBlock> m_aShikakuBlocks;

	private int[] m_size;
	private int[,] m_aRegisteredVisitedCells;
	private int[,] m_aRegisteredVisitedCellsID;


	/**
	 *	Array of weight for each range of starting points according to difficulty, total for each array = 100
	 **/
	private int[][] m_aiStartingPointsRatio = new int[][]
											{
												new int [] {    40, 10, 50      },	//	easy
												new int [] {    30, 40, 30      },  //	medium
												new int [] {    15, 50, 35      }	//	hard
											};

	private int[] m_aiCategoryRange = new int[] { 20, 60 };

	public (bool, GridModel) Generate(EDifficulty eDifficulty)
	{
		int iWidth, iHeight;
		switch (eDifficulty)
		{
			case EDifficulty.easy: { iWidth = iHeight = 5; } break;
			case EDifficulty.medium: { iWidth = iHeight = 10; } break;
			case EDifficulty.hard: { iWidth = iHeight = 20; } break;
			default: { iWidth = iHeight = 0; Assert.IsTrue(false); } break;
		}

		GridModel aGrid = new GridModel(iWidth, iHeight);

		int iStartingPointsCount = (int)(Math.Sqrt((double)(iWidth * iHeight)) * 2.0f);

		int iGenerationMaxTries = 1000;
		int iGenerationTryIndex = 0;
		bool bGenerationSuccess = false;
		while (!bGenerationSuccess && iGenerationTryIndex < iGenerationMaxTries)
		{
			bGenerationSuccess = Generate(ref aGrid, iStartingPointsCount);
			if(bGenerationSuccess)
			{
				Debug.Log(aGrid.ToString());
			}

			++iGenerationTryIndex;
		}


		return (bGenerationSuccess, aGrid);
	}

	private bool Generate(ref GridModel aGridOut, int iStartingPointsCount)
	{
		//
		// Setup
		m_size = new int[] { aGridOut.m_iHeight, aGridOut .m_iWidth};
		m_aShikakuBlocks = new List<ShikakuBlock>();

		m_aRegisteredVisitedCells = new int[m_size[AXIS_Y], m_size[AXIS_X]];
		m_aRegisteredVisitedCellsID = new int[m_size[AXIS_Y], m_size[AXIS_X]];

		//
		// Iterate for all starting points
		System.Random rnd = new System.Random();
		int iPlacingStepTriesCount = m_size[AXIS_X] * m_size[AXIS_Y];
		int iOccupyingBlock = 0;
		for (int iStartingPointIndex = 0; iStartingPointIndex < iStartingPointsCount; ++iStartingPointIndex)
		{
			int iRandomPosX = -1;
			int iRandomPosY = -1;
			int iPlacingStepTryIndex = -1;

			//
			// Check for occupation
			{
				while (IsPositionOccupied(iRandomPosX, iRandomPosY, ref iOccupyingBlock) && iPlacingStepTryIndex < iPlacingStepTriesCount)
				{
					iRandomPosX = rnd.Next(0, m_size[AXIS_X]);
					iRandomPosY = rnd.Next(0, m_size[AXIS_Y]);

					++iPlacingStepTryIndex;
				}

				if (iPlacingStepTryIndex >= iPlacingStepTriesCount)
				{
					//
					// Could not place starting point, max tries limit reached
					return false;
				}
			}

			//
			// Check for valid area and correct length from the position
			ShikakuBlock block = new ShikakuBlock();
			int[] newOrigin = new int[] { iRandomPosY, iRandomPosX };
			if (GrowBlockFromPosition(newOrigin, ref block))
			{
				//
				// Update map
				UpdateGridsWithBlock(block);

				Display(m_aRegisteredVisitedCells);
			}
			else
			{
				Debug.Log("was not able to grow block from position : (" + newOrigin[AXIS_Y].ToString() + "/" + newOrigin[AXIS_X].ToString() + ")");
			}
		}

		//
		// Grid validity
		int iValidityTry = 0;
		int iValidityTriesThreshold = 25;
		while (iValidityTry++ < iValidityTriesThreshold && !ResolveGridValidity()) { };
		if(iValidityTry >= iValidityTriesThreshold)
		{
			Debug.Log("Max validity tries occured");
		}

		//
		// Fill generated grid
		foreach (ShikakuBlock block in m_aShikakuBlocks)
		{
			// TODO Set and store area origins based on difficulty instead of using top left
			aGridOut.m_aAreaList.Add(new Area(block.pos[AXIS_X], block.pos[AXIS_Y], block.iAreaValue));
			aGridOut.m_aCells[block.pos[AXIS_X], block.pos[AXIS_Y]] = block.iAreaValue;
		}

		return true;
	}

	private bool IsPositionOccupied(int iPosX, int iPosY, ref int iOccupyingBlock)
	{
		iOccupyingBlock = -1;
		if (iPosX == -1 || iPosY == -1 || (iOccupyingBlock = m_aRegisteredVisitedCells[iPosY, iPosX]) != 0)
		{
			return true;
		}

		return false;
	}

	/**
	 *	From a position, determine all possible layouts and choose one randomly
	 **/
	private bool GrowBlockFromPosition(int[] origin, ref ShikakuBlock block)
	{
		List<int[]> aNextPositions = new List<int[]>();
		List<int[]> aVisitedPositions = new List<int[]>();
		int iCandidatesCount = aNextPositions.Count;

		//
		// Create all possible layouts filtered with the created validity map
		List<ShikakuBlock> aPossibleLayouts = CreateLayouts(origin);

		if (0 < aPossibleLayouts.Count)
		{
			//
			// choose one randomly
			System.Random rnd = new System.Random();
			int iRandomIndex = rnd.Next(0, aPossibleLayouts.Count);
			block = new ShikakuBlock(aPossibleLayouts[iRandomIndex].pos, aPossibleLayouts[iRandomIndex].size);

			return true;
		}
		else
		{
			return false;
		}
	}

	private bool IsValidNextPosition(List<int[]> aVisitedPositions, int[] position)
	{
		return (position[AXIS_X] >= 0 && position[AXIS_Y] >= 0
				&& position[AXIS_X] < m_size[AXIS_X] && position[AXIS_Y] < m_size[AXIS_Y]
				&& m_aRegisteredVisitedCells[position[AXIS_Y], position[AXIS_X]] == 0
				&& !aVisitedPositions.Contains(position)
				);
	}

	private List<ShikakuBlock> CreateOneDimensionLayouts(ShikakuBlock block, int iAxis)
	{
		List<ShikakuBlock> aHorizontalBlocks = new List<ShikakuBlock>();

		//
		// Retrieve max width
		int iOtherAxis = iAxis^1	;
		int iShikakuBlockSizeMax = 0;
		int iShikakuBlockLeftMaxOrigin = 0;
		for (int iColumnIndex = 0; iColumnIndex < m_size[iAxis]; ++iColumnIndex)
		{
			if (m_aRegisteredVisitedCells[block.pos[iOtherAxis], iColumnIndex] == -1)
			{
				if (iShikakuBlockSizeMax == 0)
				{
					iShikakuBlockLeftMaxOrigin = iColumnIndex;
				}
				++iShikakuBlockSizeMax;
			}
			else if (iColumnIndex > block.pos[iAxis] && m_aRegisteredVisitedCells[block.pos[iOtherAxis], iColumnIndex] != -1)
			{
				break;
			}
		}

		int iShikakuBlockMaxIndex = iShikakuBlockLeftMaxOrigin + iShikakuBlockSizeMax;
		int iShikakuBlockMaxOffset = block.pos[iAxis] - iShikakuBlockLeftMaxOrigin;

		//
		// Iterate from the minimum offset to the maximum
		for (int iOffset = 0; iOffset < iShikakuBlockMaxOffset; ++iOffset)
		{
			int iOrigin = block.pos[iAxis] - iOffset;
			int iMaxSize = iShikakuBlockMaxOffset - iOrigin;

			//
			// Add a layout for each possible 
			for (int iCurrentSize = 2; iCurrentSize > iMaxSize; ++iCurrentSize)
			{
				aHorizontalBlocks.Add(new ShikakuBlock(new int[] { block.pos[iOtherAxis], iOrigin }, new int[] { 1, iCurrentSize }));
			}
		}

		return aHorizontalBlocks;
	}

	private List<ShikakuBlock> CreateLayouts(int[] origin)
	{
		List<ShikakuBlock> aBlocks = new List<ShikakuBlock>();

		int iTotalArea = m_size[AXIS_X] * m_size[AXIS_Y];
		int iShikakuBlockMaxAreaThreshold = (int)(iTotalArea * 0.25f);

		//
		// Create origin lists and origin to the list
		List<ShikakuBlock> aBlocksToTest = new List<ShikakuBlock>();
		List<ShikakuBlock> aBlocksTested = new List<ShikakuBlock>();
		ShikakuBlock blockOrigin = new ShikakuBlock(origin, new int[] { 1, 1 });
		aBlocksToTest.Add(blockOrigin);

		int iTest = 0;

		while (aBlocksToTest.Count > 0 && iTest < 500)
		{
			iTest++;

			//
			// Get first index as origin to test
			ShikakuBlock blockToTest = aBlocksToTest[0];
			aBlocksToTest.RemoveAt(0);
			aBlocksTested.Add(blockToTest);

			//
			// Filter block if incorrect
			if (blockToTest.iAreaValue > iShikakuBlockMaxAreaThreshold
				|| blockToTest.pos[AXIS_Y] < 0
				|| blockToTest.pos[AXIS_X] < 0
				|| blockToTest.pos[AXIS_Y] + blockToTest.size[AXIS_Y] - 1 >= m_size[AXIS_Y]
				|| blockToTest.pos[AXIS_X] + blockToTest.size[AXIS_X] - 1 >= m_size[AXIS_X]
				)
			{
				continue;
			}
			else
			{
				bool bValidBlock = true;
				for (int iOffsetY = 0; iOffsetY < blockToTest.size[AXIS_Y]; ++iOffsetY)
				{
					for (int iOffsetX = 0; iOffsetX < blockToTest.size[AXIS_X]; ++iOffsetX)
					{
						if (m_aRegisteredVisitedCells[blockToTest.pos[AXIS_Y] + iOffsetY, blockToTest.pos[AXIS_X] + iOffsetX] != 0)
						{
							bValidBlock = false;
							break;
						}
					}

					if (!bValidBlock)
					{
						break;
					}
				}

				if (!bValidBlock)
				{
					continue;
				}
			}

			//
			// Add Block to the results
			aBlocks.Add(blockToTest);

			//
			// Try going up
			ShikakuBlock blockUp = new ShikakuBlock(new int[] { blockToTest.pos[AXIS_Y] - 1, blockToTest.pos[AXIS_X] }, blockToTest.size);
			if (blockUp.iAreaValue > 1 && !aBlocksTested.Contains(blockUp))
			{
				aBlocksToTest.Add(blockUp);
			}

			//
			// Try going right
			ShikakuBlock blockRight = new ShikakuBlock(new int[] { blockToTest.pos[AXIS_Y], blockToTest.pos[AXIS_X] + 1 }, blockToTest.size);
			if (blockRight.iAreaValue > 1 && !aBlocksTested.Contains(blockRight))
			{
				aBlocksToTest.Add(blockRight);
			}

			//
			// Try going down
			ShikakuBlock blockDown = new ShikakuBlock(new int[] { blockToTest.pos[AXIS_Y] + 1, blockToTest.pos[AXIS_X] }, blockToTest.size);
			if (blockDown.iAreaValue > 1 && !aBlocksTested.Contains(blockDown))
			{
				aBlocksToTest.Add(blockDown);
			}

			//
			// Try going left
			ShikakuBlock blockLeft = new ShikakuBlock(new int[] { blockToTest.pos[AXIS_Y], blockToTest.pos[AXIS_X] - 1 }, blockToTest.size);
			if (blockLeft.iAreaValue > 1 && !aBlocksTested.Contains(blockLeft))
			{
				aBlocksToTest.Add(blockLeft);
			}

			//
			// Try growing height
			ShikakuBlock blockHeight = new ShikakuBlock(blockToTest.pos, new int[] { blockToTest.size[AXIS_Y] + 1, blockToTest.size[AXIS_X] });
			if (!aBlocksTested.Contains(blockHeight))
			{
				aBlocksToTest.Add(blockHeight);
			}

			//
			// Try growing width
			ShikakuBlock blockWidth = new ShikakuBlock(blockToTest.pos, new int[] { blockToTest.size[AXIS_Y], blockToTest.size[AXIS_X] + 1 });
			if (!aBlocksTested.Contains(blockWidth))
			{
				aBlocksToTest.Add(blockWidth);
			}

			//
			// Try growing height and go up (for special cases like bottom-right)
			ShikakuBlock blockHeightUp = new ShikakuBlock(new int[] { blockToTest.pos[AXIS_Y] - 1, blockToTest.pos[AXIS_X] }, new int[] { blockToTest.size[AXIS_Y] + 1, blockToTest.size[AXIS_X] });
			if (!aBlocksTested.Contains(blockHeightUp))
			{
				aBlocksToTest.Add(blockHeightUp);
			}

			//
			// Try growing width and go left (for special cases like bottom-right)
			ShikakuBlock blockWidthLeft = new ShikakuBlock(new int[] { blockToTest.pos[AXIS_Y], blockToTest.pos[AXIS_X] - 1}, new int[] { blockToTest.size[AXIS_Y], blockToTest.size[AXIS_X] + 1});
			if (!aBlocksTested.Contains(blockWidthLeft))
			{
				aBlocksToTest.Add(blockWidthLeft);
			}
		}

		//
		// Remove first block which corresponds to the origin
		aBlocks.RemoveAt(0);

		return aBlocks;
	}

	public void Display(int[,] aGrid)
	{
		string strGrid = "Display Grid Generator\n";
		
		for (int iHeight = 0; iHeight < m_size[AXIS_Y]; ++iHeight)
		{
			for (int iWidth = 0; iWidth < m_size[AXIS_X]; ++iWidth)
			{
				strGrid += aGrid[iHeight, iWidth];
				strGrid += " ";
			}
			strGrid += System.Environment.NewLine;
		}
		Debug.Log(strGrid);
	}

	private bool ResolveGridValidity()
	{
		bool bValid = true;

		//
		// Check validity of grid : check presence of 0
		for (int iColumnIndex = 0; iColumnIndex<m_size[AXIS_Y]; ++iColumnIndex)
		{
			for (int iRowIndex = 0; iRowIndex<m_size[AXIS_X]; ++iRowIndex)
			{
				//
				// If empty, try to fill it
				if(0 == m_aRegisteredVisitedCells[iColumnIndex, iRowIndex])
				{
					bValid = false;

					//
					// Check for neighbouring empty cells in lines/columns

					//
					// Vertically
					int[] iPositionUp	= GrowEmptyCellTowardsDirection(AXIS_Y, -1, iColumnIndex, iRowIndex);
					int[] iPositionDown	= GrowEmptyCellTowardsDirection(AXIS_Y, +1, iColumnIndex, iRowIndex);
					if(iPositionUp[AXIS_Y] != iPositionDown[AXIS_Y])
					{
						ShikakuBlock newBlock = new ShikakuBlock(iPositionUp, new int[] { iPositionDown[AXIS_Y]-iPositionUp[AXIS_Y]+1, iPositionDown[AXIS_X] - iPositionUp[AXIS_X] + 1 });
						Debug.Log("Forming new line vertically");
						UpdateGridsWithBlock(newBlock);
					}
					else
					{
						//
						// Horizontally
						int[] iPositionLeft		= GrowEmptyCellTowardsDirection(AXIS_X, -1, iColumnIndex, iRowIndex);
						int[] iPositionRight	= GrowEmptyCellTowardsDirection(AXIS_X, +1, iColumnIndex, iRowIndex);
						if (iPositionLeft[AXIS_X] != iPositionRight[AXIS_X])
						{
							ShikakuBlock newBlock = new ShikakuBlock(iPositionLeft, new int[] { iPositionRight[AXIS_Y] - iPositionLeft[AXIS_Y] + 1,  iPositionRight[AXIS_X] - iPositionLeft[AXIS_X] + 1 });
							Debug.Log("Forming new line horizontally");
							UpdateGridsWithBlock(newBlock);
						}
						else
						{
							//
							// Try merging from another single block
							if		(!TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, -1, +0))		// Up
							{
								if (!TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, +0, +1))			// Right
								{
									if (!TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, +1, +0))		// Bottom
									{
										if (!TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, +0, -1)) // Left
										{
											//
											// Cannot merge
											Debug.Log("Cannot merge unresolved cell ("+iColumnIndex+","+iRowIndex+")");
										}
										else
										{
											Debug.Log("merged empty with left");
										}
									}
									else
									{
										Debug.Log("merged empty with bottom");
									}
								}
								else
								{
									Debug.Log("merged empty with right");
								}
							}
							else
							{
								Debug.Log("merged empty with top");
							}
						}
					}
				}
			}
		}

		return bValid;
	}

	private int[] GrowEmptyCellTowardsDirection(int iAxis, int iDirection, int iOriginY, int iOriginX)
	{
		int[] iFinalPosition = new int[] { iOriginY, iOriginX };
		int[] iPositionToTest = new int[] { iOriginY, iOriginX};
		while ((iPositionToTest[iAxis] = iPositionToTest[iAxis] + iDirection) >= 0
			&&	iPositionToTest[iAxis^1] >= 0
			&&	iPositionToTest[AXIS_Y] < m_size[AXIS_Y]
			&&	iPositionToTest[AXIS_X] < m_size[AXIS_X]
			&&	0 == m_aRegisteredVisitedCells[iPositionToTest[AXIS_Y], iPositionToTest[AXIS_X]])
		{
			iFinalPosition = new int[] { iPositionToTest[AXIS_Y], iPositionToTest[AXIS_X] };
		}

		return iFinalPosition;
	}

	private bool TryMergingEmptyCellWithNeighbour(int iOriginY, int iOriginX, int iDirectionY, int iDirectionX)
	{
		int iNextPosY = iOriginY + iDirectionY;
		int iNextPosX = iOriginX + iDirectionX;
		if(		iNextPosY >= 0
			&&	iNextPosX >= 0
			&&	iNextPosY < m_size[AXIS_Y]
			&&	iNextPosX < m_size[AXIS_X])
		{
			int iBlockIndex = m_aRegisteredVisitedCellsID[iNextPosY, iNextPosX] - 1;
			if(iBlockIndex >= m_aShikakuBlocks.Count || iBlockIndex < 0)
			{
				Debug.Log("while merging, trying to access index '"+iBlockIndex+"' from aShikakuBlocks");
			}
			ShikakuBlock block = m_aShikakuBlocks[iBlockIndex];
			if (	block.size[AXIS_X] == 1 && iDirectionY != 0
				||	block.size[AXIS_Y] == 1 && iDirectionX != 0)
			{
				ShikakuBlock newBlock = new ShikakuBlock(
					new int[] { Math.Min(iOriginY, block.pos[AXIS_Y]), Math.Min(iOriginX, block.pos[AXIS_X]) },
					new int[] { block.size[AXIS_Y] + Math.Abs(iDirectionY), block.size[AXIS_X] + Math.Abs(iDirectionX) });

				Debug.Log("iblockindex to replace : " + iBlockIndex);
				UpdateGridsWithMergedBlock(newBlock, iBlockIndex);

				return true;
			}
		}
		return false;
	}

	private void UpdateGridsWithBlock(ShikakuBlock block)
	{
		if(block.iAreaValue < 0)
		{
			Debug.Log("");
		}
		m_aShikakuBlocks.Add(block);

		Debug.Log("before updating (adding block : "+block+") :");
		Display(m_aRegisteredVisitedCellsID);

		int iMaxIndexHeight = block.pos[AXIS_Y] + block.size[AXIS_Y];
		int iMaxIndexWidth = block.pos[AXIS_X] + block.size[AXIS_X];
		for (int iColumnIndex = block.pos[AXIS_Y]; iColumnIndex < iMaxIndexHeight; ++iColumnIndex)
		{
			for (int iRowIndex = block.pos[AXIS_X]; iRowIndex < iMaxIndexWidth; ++iRowIndex)
			{
				m_aRegisteredVisitedCells[iColumnIndex, iRowIndex] = block.iAreaValue;
				m_aRegisteredVisitedCellsID[iColumnIndex, iRowIndex] = m_aShikakuBlocks.Count;
			}
		}
		Debug.Log("after updating :");
		Display(m_aRegisteredVisitedCellsID);
	}

	private void UpdateGridsWithMergedBlock(ShikakuBlock block, int iBlockIndextoReplace)
	{
		m_aShikakuBlocks[iBlockIndextoReplace] = block;

		Debug.Log("before merging (adding block : " + block + ") of index '"+iBlockIndextoReplace+"' :");
		Display(m_aRegisteredVisitedCellsID);

		int iMaxIndexHeight = block.pos[AXIS_Y] + block.size[AXIS_Y];
		int iMaxIndexWidth = block.pos[AXIS_X] + block.size[AXIS_X];
		if (iMaxIndexHeight > m_aRegisteredVisitedCells.GetLength(0)
		|| iMaxIndexWidth > m_aRegisteredVisitedCells.GetLength(1))
		{
			Debug.Log("");
		}
		for (int iColumnIndex = block.pos[AXIS_Y]; iColumnIndex < iMaxIndexHeight; ++iColumnIndex)
		{
			for (int iRowIndex = block.pos[AXIS_X]; iRowIndex < iMaxIndexWidth; ++iRowIndex)
			{
				m_aRegisteredVisitedCells[iColumnIndex, iRowIndex] = block.iAreaValue;
				m_aRegisteredVisitedCellsID[iColumnIndex, iRowIndex] = iBlockIndextoReplace+1;
			}
		}
		Debug.Log("after merging:");
		Display(m_aRegisteredVisitedCellsID);
	}
}
