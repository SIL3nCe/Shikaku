using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System;
public enum EDifficulty
{
	easy,
	medium,
	hard,
}

class GridGenerator
{


	private enum EStartingPointCategory
	{
		small,
		medium,
		large,
	}

	public static int AXIS_Y = 0;
	public static int AXIS_X = 1;
	public static string[] astrAxis = {"Y", "X"};

	public static int[] directionTop	= new int[] { -1, +0 };
	public static int[] directionRight	= new int[] { +0, +1 };
	public static int[] directionBottom = new int[] { +1, +0 };
	public static int[] directionLeft	= new int[] { +0, -1 };

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

		int iGenerationMaxTries = 10;
		int iGenerationTryIndex = 0;
		bool bGenerationSuccess = false;
		while (!bGenerationSuccess && iGenerationTryIndex++ < iGenerationMaxTries)
		{
			bGenerationSuccess = Generate(ref aGrid);
			if(bGenerationSuccess)
			{
				Log(aGrid.ToString());
			}
			else
			{
				Assert.IsTrue(false);
			}
		}


		return (bGenerationSuccess, aGrid);
	}

	private bool Generate(ref GridModel aGridOut)
	{
		//
		// Setup
		m_size = new int[] { aGridOut.m_iHeight, aGridOut .m_iWidth};
		m_aShikakuBlocks = new List<ShikakuBlock>();

		m_aRegisteredVisitedCells = new int[m_size[AXIS_Y], m_size[AXIS_X]];
		m_aRegisteredVisitedCellsID = new int[m_size[AXIS_Y], m_size[AXIS_X]];

		//
		// Iterate 'width*height' times to fill the grid with starting points
		System.Random rnd = new System.Random();
		int iPlacingStepTriesCount = m_size[AXIS_X] * m_size[AXIS_Y];
		int iOccupyingBlock = 0;
		int iStartingPointIndex = 0;
		while (!IsGridComplete() && iStartingPointIndex++ < iPlacingStepTriesCount)
		{
			int iRandomPosX = rnd.Next(0, m_size[AXIS_X]);
			int iRandomPosY = rnd.Next(0, m_size[AXIS_Y]);

			//
			// Check for occupation
			if(IsPositionOccupied(iRandomPosX, iRandomPosY, ref iOccupyingBlock))
			{
				continue;
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

			}
		}

		Log("After adding initial starting points :");
		Display(m_aRegisteredVisitedCellsID);

		//
		// Grid validity
		int iValidityTry = 0;
		int iValidityTriesThreshold = (int)(m_size[AXIS_X] * m_size[AXIS_Y] * 0.5f);
		while (iValidityTry++ < iValidityTriesThreshold && !ResolveGridValidity()) { };
		if(iValidityTry >= iValidityTriesThreshold)
		{
			Log("Max validity tries occured");
		}

		if (!IsGridComplete())
		{
			Log("----------> grid not complete !!!");
			Assert.IsTrue(false);
			return false;
		}

		if (!IsGridValid())
		{
			Log("----------> grid not valid !!!");
			Assert.IsTrue(false);
			return false;
		}

		//
		// Fill generated grid
		foreach (ShikakuBlock block in m_aShikakuBlocks)
		{
			if(block.iAreaValue > 0)
			{
				// TODO Set and store area origins based on difficulty instead of using top left
				int[] iNewPos = new int[2] {	rnd.Next(block.pos[AXIS_X], block.pos[AXIS_X] + block.size[AXIS_X]),
												rnd.Next(block.pos[AXIS_Y], block.pos[AXIS_Y] + block.size[AXIS_Y]) };
				aGridOut.m_aAreaList.Add(new Area(iNewPos[AXIS_X], iNewPos[AXIS_Y], block.iAreaValue));
				aGridOut.m_aCells[iNewPos[AXIS_X], iNewPos[AXIS_Y]] = block.iAreaValue;
			}
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

	private bool IsValidPositionInGrid(int iPosY, int iPosX)
	{
		return (	iPosY >= 0 && iPosX >= 0
				&&	iPosY < m_size[AXIS_Y] && iPosX < m_size[AXIS_X]
				);
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
				strGrid += "\t";
			}
			strGrid += System.Environment.NewLine;
		}
		Log(strGrid);
	}

	private bool ResolveGridValidity()
	{
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
					//
					// Check for neighbouring empty cells in lines/columns

					//
					// Grow cell vertically (only 0)
					if(!GrowEmptyCellTowardsAxis(AXIS_Y, iColumnIndex, iRowIndex))
					{
						//
						// Grow cell horizontally (only 0)
						if (!GrowEmptyCellTowardsAxis(AXIS_X, iColumnIndex, iRowIndex))
						{
							//
							// Try merging from a linear block
							if (!TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, -1, +0))             // Up
							{
								if (!TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, +0, +1))         // Right
								{
									if (!TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, +1, +0))     // Bottom
									{
										if (!TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, +0, -1)) // Left
										{
											//
											// If merging is not possible, split a neighbour to create a favorable
											// merging for next iteration
											if(!SplitNeighbour(iColumnIndex, iRowIndex))
											{
												//
												// Splitting neighbours is impossible because only blocks of 2 surround the empty cell
												// Empty the cells in the 8-neighbourhood of the empty cell
												if(!EmptyCellNeighbourhood(iColumnIndex, iRowIndex))
												{
													Log("This should never happen, merge could not be done.");
												}
											}
										}
										else
										{
											Log("merged empty with left");
										}
									}
									else
									{
										Log("merged empty with bottom");
									}
								}
								else
								{
									Log("merged empty with right");
								}
							}
							else
							{
								Log("merged empty with top");
							}
						}
					}
					return false;
				}
			}
		}

		return true;
	}

	private bool GrowEmptyCellTowardsAxis(int iAxis, int iOriginY, int iOriginX)
	{
		int[] iPositionM = GrowEmptyCellTowardsDirection(iAxis, -1, iOriginY, iOriginX);
		int[] iPositionP = GrowEmptyCellTowardsDirection(iAxis, +1, iOriginY, iOriginX);
		if (iPositionM[iAxis] != iPositionP[iAxis])
		{
			int iOtherAxis = iAxis ^ 1;
			ShikakuBlock newBlock = new ShikakuBlock();
			newBlock.pos = iPositionM;
			newBlock.size[iOtherAxis] = iPositionP[iOtherAxis] - iPositionM[iOtherAxis] + 1;
			newBlock.size[iAxis] = iPositionP[iAxis] - iPositionM[iAxis] + 1;
			newBlock.iAreaValue = newBlock.size[iAxis] * newBlock.size[iOtherAxis];

			Log("Forming new line along axis "+astrAxis[iAxis]);
			UpdateGridsWithBlock(newBlock);

			return true;
		}

		return false;
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
				Log("while merging, trying to access index '"+iBlockIndex+"' from aShikakuBlocks");
			}
			ShikakuBlock block = m_aShikakuBlocks[iBlockIndex];
			if (	(block.size[AXIS_Y] == 1 && iDirectionX != 0)
				||	(block.size[AXIS_X] == 1 && iDirectionY != 0))
			{
				ShikakuBlock newBlock = new ShikakuBlock(
					new int[] { Math.Min(iOriginY, block.pos[AXIS_Y]), Math.Min(iOriginX, block.pos[AXIS_X]) },
					new int[] { block.size[AXIS_Y] + Math.Abs(iDirectionY), block.size[AXIS_X] + Math.Abs(iDirectionX) });
				if(newBlock.size[AXIS_X]+newBlock.pos[AXIS_X] -1 >= m_size[AXIS_X] || newBlock.size[AXIS_Y] + newBlock.pos[AXIS_Y] -1 >= m_size[AXIS_Y])
				{
					Log("bigger");
				}
				Log("iblockindex to replace : " + iBlockIndex);
				UpdateGridsWithMergedBlock(newBlock, iBlockIndex);

				return true;
			}
		}
		return false;
	}

	private bool SplitNeighbour(int iColumnIndex, int iRowIndex)
	{
		Log("Splitting neighbours");
		Display(m_aRegisteredVisitedCellsID);
		bool bMerge = false;
		int[] origin = new int[]{ iColumnIndex, iRowIndex };

		// 
		// Top
		if (SplitNeighbourDirection(origin, directionTop, AXIS_Y, ref bMerge))
		{
			Log("Top");
			if(bMerge)
			{
				if (TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, -1, +0))
				{
					return true;
				}
				else
				{
					Log("Could not merge cell after splitting neighbour, THIS IS NOT NORMAL !!");
				}
			}
			else
			{
				if (GrowEmptyCellTowardsAxis(AXIS_Y, iColumnIndex, iRowIndex))
				{
					return true;
				}
				else
				{
					Log("Could not grow cell after freeing space, THIS IS NOT NORMAL !!");
				}
			}
		}
		else
		{ 
			// 
			// Right
			if (SplitNeighbourDirection(origin, directionRight, AXIS_X, ref bMerge))
			{
				Log("Right");
				if (bMerge)
				{
					if (TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, +0, +1))
					{
						return true;
					}
					else
					{
						Log("Could not merge cell after splitting neighbour, THIS IS NOT NORMAL !!");
					}
				}
				else
				{
					if (GrowEmptyCellTowardsAxis(AXIS_X, iColumnIndex, iRowIndex))
					{
						return true;
					}
					else
					{
						Log("Could not grow cell after freeing space, THIS IS NOT NORMAL !!");
					}
				}
			}
			else
			{
				// 
				// Bottom
				if (SplitNeighbourDirection(origin, directionBottom, AXIS_Y, ref bMerge))
				{
					Log("Bottom");
					if(bMerge)
					{
						if (TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, +1, +0))
						{
							return true;
						}
						else
						{
							Log("Could not merge cell after splitting neighbour, THIS IS NOT NORMAL !!");
						}
					}
					else
					{
						if (GrowEmptyCellTowardsAxis(AXIS_Y, iColumnIndex, iRowIndex))
						{
							return true;
						}
						else
						{
							Log("Could not grow cell after freeing space, THIS IS NOT NORMAL !!");
						}
					}
				}
				else
				{
					// 
					// Left
					if (SplitNeighbourDirection(origin, directionLeft, AXIS_X, ref bMerge))
					{
						Log("Left");
						if(bMerge)
						{
							if (TryMergingEmptyCellWithNeighbour(iColumnIndex, iRowIndex, +0, -1))
							{
								return true;
							}
							else
							{
								Log("Could not merge cell after splitting neighbour, THIS IS NOT NORMAL !!");
							}
						}
						else
						{
							if (GrowEmptyCellTowardsAxis(AXIS_X, iColumnIndex, iRowIndex))
							{
								return true;
							}
							else
							{
								Log("Could not grow cell after freeing space, THIS IS NOT NORMAL !!");
							}
						}
					}
				}
			}
		}

		return false;
	}

	private bool SplitNeighbourDirection(int[] origin, int[] direction, int iAxis, ref bool bMerge)
	{
		int[] nextPos = new int[] { 0,0 };
		if(IsValidPosition(origin, direction, ref nextPos))
		{
			//
			// Retrieve block to split
			int iShikakuBlockIndex = m_aRegisteredVisitedCellsID[nextPos[AXIS_Y], nextPos[AXIS_X]] - 1;
			ShikakuBlock block = m_aShikakuBlocks[iShikakuBlockIndex];
			int iOtherAxis = iAxis ^ 1;

			//
			// Check if nextPos belongs to a rectangle, if so split it to have
			// a line/column in nextPos which will be then split again.
			if (block.size[iOtherAxis] > 1 && block.size[iAxis] > 1)
			{
				Log("Splitting neighbour rectangle");

				//
				// First look for split blocks except the one in the merge direction
				List<ShikakuBlock> aBlockToAdd = new List<ShikakuBlock>();
				if (nextPos[iOtherAxis] > block.pos[iOtherAxis])   // If line.pos to split > than block.pos
				{
					ShikakuBlock blockAxisM = new ShikakuBlock();
					blockAxisM.pos = block.pos;
					blockAxisM.size[iOtherAxis] = nextPos[iOtherAxis] - block.pos[iOtherAxis];
					blockAxisM.size[iAxis] = block.size[iAxis];
					blockAxisM.iAreaValue = blockAxisM.size[iAxis] * blockAxisM.size[iOtherAxis];

					aBlockToAdd.Add(blockAxisM);

					Log("--> M : "+ blockAxisM);
				}
				if (nextPos[iOtherAxis] < block.pos[iOtherAxis] + block.size[iOtherAxis] - 1) // if line to split corresponds to bottom/right
				{
					ShikakuBlock blockAxisP = new ShikakuBlock();
					blockAxisP.pos[iOtherAxis] = nextPos[iOtherAxis]+1;
					blockAxisP.pos[iAxis] = Math.Min(nextPos[iAxis], block.pos[iAxis]);
					blockAxisP.size[iOtherAxis] = block.pos[iOtherAxis] + block.size[iOtherAxis] - nextPos[iOtherAxis] - 1;
					blockAxisP.size[iAxis] = block.size[iAxis];
					blockAxisP.iAreaValue = blockAxisP.size[iAxis] * blockAxisP.size[iOtherAxis];

					aBlockToAdd.Add(blockAxisP);

					Log("--> P : " + blockAxisP);
				}

				//
				// Then split from nextPos along iAxis
				ShikakuBlock blockReplacing = new ShikakuBlock();
				blockReplacing.pos[iAxis] = Math.Min(nextPos[iAxis], block.pos[iAxis]);
				blockReplacing.pos[iOtherAxis] = nextPos[iOtherAxis];
				blockReplacing.size[iOtherAxis] = 1;
				blockReplacing.size[iAxis] = block.size[iAxis];
				blockReplacing.iAreaValue = blockReplacing.size[iAxis] * blockReplacing.size[iOtherAxis];
				UpdateGridsWithMergedBlock(blockReplacing, iShikakuBlockIndex);

				//
				// Uupdate block reference to new block
				block = blockReplacing;
				Log("--> replacing : " + block);

				//
				// Finally add all remaining blocks
				for (int iBlockToAddIndex = 0; iBlockToAddIndex < aBlockToAdd.Count; ++iBlockToAddIndex)
				{
					UpdateGridsWithBlock(aBlockToAdd[iBlockToAddIndex]);
				}
			}
			
			//
			// Check size before splitting in iOtherAxis way
			if (block.size[iAxis] == 1 && block.size[iOtherAxis] > 2)
			{
				Log("Split iOtherAxis");

				//
				// Check splitting consistency by creating new blocks
				List<ShikakuBlock> aSplitBlocks = new List<ShikakuBlock>();
				if(block.pos[iOtherAxis] == nextPos[iOtherAxis])	// neighbour corresponds to top-left
				{
					ShikakuBlock newBlock = new ShikakuBlock();
					newBlock.pos[iAxis] =  block.pos[iAxis];
					newBlock.pos[iOtherAxis] = block.pos[iOtherAxis] + 1;
					newBlock.size[iOtherAxis] = block.size[iOtherAxis] - 1;
					newBlock.size[iAxis] = block.size[iAxis];
					newBlock.iAreaValue = newBlock.size[iAxis] * newBlock.size[iOtherAxis];

					aSplitBlocks.Add(newBlock);

					Log("Top-left : "+newBlock);
				}
				else if(nextPos[iOtherAxis] == block.pos[iOtherAxis] + block.size[iOtherAxis] -1)	// neighbour corresponds to max size
				{
					ShikakuBlock newBlock = new ShikakuBlock();
					newBlock.pos = block.pos;
					newBlock.size[iOtherAxis] = block.size[iOtherAxis] - 1;
					newBlock.size[iAxis] = block.size[iAxis];
					newBlock.iAreaValue = newBlock.size[iAxis] * newBlock.size[iOtherAxis];

					aSplitBlocks.Add(newBlock);

					Log("bottom-right: " + newBlock);
				}
				else        // neighbour belons to center needing sub-splitting
				{

					Log("middle");
					//
					// First check if partitionning size doesn't leave single cells
					int iMaxSizeIndex = block.pos[iOtherAxis] + block.size[iOtherAxis] - 1;
					if(nextPos[iOtherAxis] - block.pos[iOtherAxis] > 1 && iMaxSizeIndex - nextPos[iOtherAxis] > 1)
					{
						ShikakuBlock newBlock1 = new ShikakuBlock();
						newBlock1.pos = block.pos;
						newBlock1.size[iOtherAxis] = nextPos[iOtherAxis] - block.pos[iOtherAxis];
						newBlock1.size[iAxis] = block.size[iAxis];
						newBlock1.iAreaValue = newBlock1.size[iAxis] * newBlock1.size[iOtherAxis];

						ShikakuBlock newBlock2 = new ShikakuBlock();
						newBlock2.pos[iAxis] = nextPos[iAxis];
						newBlock2.pos[iOtherAxis] = nextPos[iOtherAxis] + 1;
						newBlock2.size[iAxis] = block.size[iAxis];
						newBlock2.size[iOtherAxis] = iMaxSizeIndex - nextPos[iOtherAxis];
						newBlock2.iAreaValue = newBlock2.size[iAxis] * newBlock2.size[iOtherAxis];

						aSplitBlocks.Add(newBlock1);
						aSplitBlocks.Add(newBlock2);
					}
					else
					{
						Log("");
					}
				}

				//
				// Update nextPos to 0
				m_aRegisteredVisitedCells[nextPos[AXIS_Y], nextPos[AXIS_X]] = 0;
				m_aRegisteredVisitedCellsID[nextPos[AXIS_Y], nextPos[AXIS_X]] = 0;

				//
				// First block : replace existing one
				UpdateGridsWithMergedBlock(aSplitBlocks[0], iShikakuBlockIndex);
				if (aSplitBlocks.Count > 1)
				{
					UpdateGridsWithBlock(aSplitBlocks[1]);
				}

				bMerge = false;
				return true;
			}
			else
			{
				bMerge = true;
				return false;
			}
		}
		return false;
	}

	private bool IsValidPosition(int[] origin, int[] direction, ref int[] nextPos)
	{
		nextPos[AXIS_Y] = origin[AXIS_Y] + direction[AXIS_Y];
		nextPos[AXIS_X] = origin[AXIS_X] + direction[AXIS_X];
		return nextPos[AXIS_Y] >= 0
			&& nextPos[AXIS_X] >= 0
			&& nextPos[AXIS_Y] < m_size[AXIS_Y]
			&& nextPos[AXIS_X] < m_size[AXIS_X];
	}

	private void UpdateGridsWithBlock(ShikakuBlock block)
	{
		if(block.iAreaValue < 0)
		{
			Log("");
		}
		m_aShikakuBlocks.Add(block);

		//Log("before updating (adding block : "+block+") :");
		//Display(m_aRegisteredVisitedCellsID);

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
		//Log("after updating :");
		//Display(m_aRegisteredVisitedCellsID);

		if (!IsGridValid())
		{
			Assert.IsTrue(false);
		}
	}

	private void UpdateGridsWithMergedBlock(ShikakuBlock block, int iBlockIndextoReplace)
	{
		m_aShikakuBlocks[iBlockIndextoReplace] = block;

		Log("before merging (adding block : " + block + ") of index '"+iBlockIndextoReplace+"' :");
		Display(m_aRegisteredVisitedCellsID);

		int iMaxIndexHeight = block.pos[AXIS_Y] + block.size[AXIS_Y] - 1;
		int iMaxIndexWidth = block.pos[AXIS_X] + block.size[AXIS_X] - 1;
		if (iMaxIndexHeight > m_aRegisteredVisitedCells.GetLength(0)
		|| iMaxIndexWidth > m_aRegisteredVisitedCells.GetLength(1)
		|| iMaxIndexWidth >= m_size[AXIS_X]
		|| iMaxIndexHeight >= m_size[AXIS_Y])
		{
			Log("");
		}
		for (int iColumnIndex = block.pos[AXIS_Y]; iColumnIndex <= iMaxIndexHeight; ++iColumnIndex)
		{
			for (int iRowIndex = block.pos[AXIS_X]; iRowIndex <= iMaxIndexWidth; ++iRowIndex)
			{
				m_aRegisteredVisitedCells[iColumnIndex, iRowIndex] = block.iAreaValue;
				m_aRegisteredVisitedCellsID[iColumnIndex, iRowIndex] = iBlockIndextoReplace+1;
			}
		}
		Log("after merging:");
		Display(m_aRegisteredVisitedCellsID);

		if(!IsGridValid())
		{
			Assert.IsTrue(false);
		}
	}

	public static void TestValidityNeighbourSplitting()
	{
		GridGenerator generator = new GridGenerator();

		generator.m_size = new int[] {5,5};
		generator.m_aRegisteredVisitedCells = new int[,]
		{
			{4, 4, 4, 4, 2},
			{6, 6, 6, 0, 2},
			{6, 6, 6, 6, 6},
			{2, 4, 4, 6, 6},
			{2, 4, 4, 6, 6}
		};
		generator.m_aRegisteredVisitedCellsID = new int[,]
		{
			{4, 4, 4, 4, 2},
			{6, 6, 6, 0, 2},
			{6, 6, 6, 3, 3},
			{1, 5, 5, 3, 3},
			{1, 5, 5, 3, 3}
		};
		generator.m_aShikakuBlocks = new List<ShikakuBlock>();
		generator.m_aShikakuBlocks.Add(new ShikakuBlock(new int[] {3,0}, new int[] {2,1}));
		generator.m_aShikakuBlocks.Add(new ShikakuBlock(new int[] {0,4}, new int[] {2,1}));
		generator.m_aShikakuBlocks.Add(new ShikakuBlock(new int[] {2,3}, new int[] {3,2}));
		generator.m_aShikakuBlocks.Add(new ShikakuBlock(new int[] {0,0}, new int[] {1,4}));
		generator.m_aShikakuBlocks.Add(new ShikakuBlock(new int[] {3,1}, new int[] {2,2}));
		generator.m_aShikakuBlocks.Add(new ShikakuBlock(new int[] {1,0}, new int[] {2,3}));

		bool bValid;
		bValid = generator.ResolveGridValidity();
		Assert.IsFalse(bValid);
		bValid = generator.ResolveGridValidity();
		Assert.IsTrue(bValid);
	}

	private bool EmptyCellNeighbourhood(int iColumnIndex, int iRowIndex)
	{
		Log("Empty cell neighbourhood with :");
		Display(m_aRegisteredVisitedCellsID);

		int[] origin = new int[] { iColumnIndex, iRowIndex};

		//
		// Free blocks
		List<int> aBlockIDs = new List<int>();

		//
		// Top
		if(IsValidPositionInGrid(iColumnIndex - 1, iRowIndex))
		{
			aBlockIDs.Add(m_aRegisteredVisitedCellsID[iColumnIndex - 1, iRowIndex] - 1);
		}

		//
		// Right
		if (IsValidPositionInGrid(iColumnIndex, iRowIndex + 1))
		{
			aBlockIDs.Add(m_aRegisteredVisitedCellsID[iColumnIndex, iRowIndex + 1] - 1);
		}

		//
		// Bottom
		if (IsValidPositionInGrid(iColumnIndex + 1, iRowIndex))
		{
			aBlockIDs.Add(m_aRegisteredVisitedCellsID[iColumnIndex + 1, iRowIndex] - 1);
		}

		//
		// Left
		if (IsValidPositionInGrid(iColumnIndex, iRowIndex - 1))
		{
			aBlockIDs.Add(m_aRegisteredVisitedCellsID[iColumnIndex, iRowIndex - 1] - 1);
		}

		//
		// Iterate inside the neighbourhood rectangle to free blocks
		for(int iBlockIDIndex = 0; iBlockIDIndex < aBlockIDs.Count; ++iBlockIDIndex)
		{
			ShikakuBlock block = m_aShikakuBlocks[aBlockIDs[iBlockIDIndex]];
			int iMaxHeightIndex = block.pos[AXIS_Y] + block.size[AXIS_Y] - 1;
			int iMaxWidthIndex = block.pos[AXIS_X] + block.size[AXIS_X] - 1;
			for (int iNeighbourhoodColumnIndex = block.pos[AXIS_Y]; iNeighbourhoodColumnIndex <= iMaxHeightIndex; ++iNeighbourhoodColumnIndex)
			{
				for (int iNeighbourhoodRowIndex = block.pos[AXIS_X]; iNeighbourhoodRowIndex <= iMaxWidthIndex; ++iNeighbourhoodRowIndex)
				{
					//
					//  Empty emplacement
					m_aRegisteredVisitedCells[iNeighbourhoodColumnIndex, iNeighbourhoodRowIndex] = 0;
					m_aRegisteredVisitedCellsID[iNeighbourhoodColumnIndex, iNeighbourhoodRowIndex] = 0;
				}
			}
			block.iAreaValue = 0;
			block.size[AXIS_X] = 0;
			block.size[AXIS_Y] = 0;
		}

		Log("Removed neighbours  :"+ aBlockIDs);
		Display(m_aRegisteredVisitedCellsID);

		return true;
	}

	private bool IsGridValid()
	{
		//
		// Iterate inside the neighbourhood rectangle 
		for (int iBlockIDIndex = 0; iBlockIDIndex < m_aShikakuBlocks.Count; ++iBlockIDIndex)
		{
			ShikakuBlock block = m_aShikakuBlocks[iBlockIDIndex];
			if(block.iAreaValue == 0)
			{
				continue;
			}

			int iMaxHeightIndex = block.pos[AXIS_Y] + block.size[AXIS_Y] - 1;
			int iMaxWidthIndex = block.pos[AXIS_X] + block.size[AXIS_X] - 1;
			for (int iNeighbourhoodColumnIndex = block.pos[AXIS_Y]; iNeighbourhoodColumnIndex <= iMaxHeightIndex; ++iNeighbourhoodColumnIndex)
			{
				for (int iNeighbourhoodRowIndex = block.pos[AXIS_X]; iNeighbourhoodRowIndex <= iMaxWidthIndex; ++iNeighbourhoodRowIndex)
				{
					if(		m_aRegisteredVisitedCells[iNeighbourhoodColumnIndex,iNeighbourhoodRowIndex] != block.iAreaValue 
						||	m_aRegisteredVisitedCellsID[iNeighbourhoodColumnIndex,iNeighbourhoodRowIndex] != iBlockIDIndex+1)
					{
						return false;
					}
				}
			}
		}

		//Log("Grid valid !");

		return true;
	}


	private bool IsGridComplete()
	{
		//
		// Iterate inside the neighbourhood rectangle 

		for (int iRow = 0; iRow < m_size[AXIS_Y]; ++iRow)
		{
			for (int iColumn = 0; iColumn < m_size[AXIS_X]; ++iColumn)
			{
				if (	m_aRegisteredVisitedCells[iRow, iColumn] == 0
					||	m_aRegisteredVisitedCellsID[iRow, iColumn] == 0)
				{
					return false;
				}
			}
		}

		Log("Grid complete !");

		return true;
	}

	private void Log(string str)
	{
		//Console.WriteLine(str);
		Debug.Log(str);
		System.Diagnostics.Debug.WriteLine(str);
	}
}
