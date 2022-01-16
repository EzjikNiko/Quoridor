using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Assets.Utility;

namespace Assets.Scripts
{
    public class Board
    {
        public const int MAX_PLAYERS = 2;
        public const int MIN_PLAYERS = 2;
        public const int BOARD_SIZE = 9;
        public const int TOTAL_WALLS = 20;

        int _numPlayers;

        public PlayerInfo[] playerStatus;
        public gameSquareInfo[,] boardStatus;
        public WallPeg[,] wallPegStatus;
        public int[,] accessible;

        private Board()
        {
            boardStatus = new gameSquareInfo[BOARD_SIZE, BOARD_SIZE];
            wallPegStatus = new WallPeg[BOARD_SIZE - 1, BOARD_SIZE - 1];
            accessible = new int[BOARD_SIZE, BOARD_SIZE];
        }

        public Board(int numPlayers) : this()
        {
            _numPlayers = numPlayers;
            playerStatus = new PlayerInfo[numPlayers];
        }

        public Board(Board other) : this(other._numPlayers)
        {
            accessible = (int[,])other.accessible.Clone();
            for (int i = 0; i < _numPlayers; i++)
            {
                playerStatus[i] = new PlayerInfo(other.playerStatus[i]);
            }
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                for (int j = 0; j < BOARD_SIZE; j++)
                {
                    if (i < BOARD_SIZE - 1 && j < BOARD_SIZE - 1)
                    {
                        wallPegStatus[i, j] = new WallPeg(other.wallPegStatus[i, j]);
                    }
                    boardStatus[i, j] = new gameSquareInfo(other.boardStatus[i, j]);
                }
            }
        }

        #region Logic for checking if a move is legal
        public bool CheckWallH(int xPos, int yPos)
        {
            if (!boardStatus[xPos, yPos].hasBotWall &&
                !boardStatus[xPos, yPos + 1].hasBotWall &&
                 wallPegStatus[xPos, yPos].isOpen)
            {
				bool retVal;
				PlaceHorizontalWall(this, -1, xPos, yPos);
                if (_numPlayers == 2 && CheckWinnable(0) && CheckWinnable(1))
                {
					retVal = true;
                }
                else if (_numPlayers == 4 && CheckWinnable(0) && CheckWinnable(1) &&
                    CheckWinnable(2) && CheckWinnable(3))
                {
                    retVal =  true;
                }
                else
                {
					retVal = false;
                }
				UndoPlaceHorizontalWall (this, -1, xPos, yPos);
				return retVal;
            }
            else
            {
                return false;
            }

        }

        public bool CheckWinnable(int id)
        {
			return AStarSearch.FindShortestPathLength(this, id) != -1;
        }

        public bool CheckWallV(int xPos, int yPos)
        {
            if (!boardStatus[xPos, yPos].hasRightWall &&
                !boardStatus[xPos + 1, yPos].hasRightWall &&
                wallPegStatus[xPos, yPos].isOpen)
            {
				Board.PlaceVerticalWall(this, -1, xPos, yPos);
				bool retVal;
                if (_numPlayers == 2 && CheckWinnable(0) && CheckWinnable(1))
                {
                    retVal = true;
                }
                else if (_numPlayers == 4 && CheckWinnable(0) && CheckWinnable(1) &&
                    CheckWinnable(2) && CheckWinnable(3))
                {
                    retVal = true;
                }
                else
                {
					retVal = false;
                }
				UndoPlaceVerticalWall (this, -1, xPos, yPos);
				return retVal;
            }
            else
            {
                return false;
            }

        }

        #endregion
        
        #region Functions representing player's decision(moving or placing wall)
        public delegate void Action(Board b, int p, int x, int y);

        public void ExecuteFunction(ActionFunction actionFunction)
        {
            Action action = actionFunction.function;
            action.Invoke(this, actionFunction.player, actionFunction.x, actionFunction.y);
        }

        public static void MovePawn(Board board, int player, int newX, int newY)
        {
            board.boardStatus[board.playerStatus[player].x, board.playerStatus[player].y].isOpen = true;
            board.boardStatus[newX, newY].isOpen = false;
            board.playerStatus[player].x = newX;
            board.playerStatus[player].y = newY;
        }

        public static void UndoMovePawn(Board board, int player, int oldX, int oldY)
        {
            board.boardStatus[board.playerStatus[player].x, board.playerStatus[player].y].isOpen = true;
            board.boardStatus[oldX, oldY].isOpen = false;
            board.playerStatus[player].x = oldX;
            board.playerStatus[player].y = oldY;
        }

        public static void PlaceHorizontalWall(Board board, int player, int x, int y)
        {
            board.wallPegStatus[x, y].isOpen = false;
            board.boardStatus[x, y].hasBotWall = true;
            board.boardStatus[x, y + 1].hasBotWall = true;
			if (player != -1)
				board.playerStatus[player].wallsLeft--;
        }

        public static void UndoPlaceHorizontalWall(Board board, int player, int x, int y)
        {
            board.wallPegStatus[x, y].isOpen = true;
            board.boardStatus[x, y].hasBotWall = false;
            board.boardStatus[x, y + 1].hasBotWall = false;
			if (player != -1)
				board.playerStatus[player].wallsLeft++;
        }

        public static void PlaceVerticalWall(Board board, int player, int x, int y)
        {
            board.wallPegStatus[x, y].isOpen = false;
            board.boardStatus[x, y].hasRightWall = true;
            board.boardStatus[x + 1, y].hasRightWall = true;
			if (player != -1)
            	board.playerStatus[player].wallsLeft--;
        }

        public static void UndoPlaceVerticalWall(Board board, int player, int x, int y)
        {
            board.wallPegStatus[x, y].isOpen = true;
            board.boardStatus[x, y].hasRightWall = false;
            board.boardStatus[x + 1, y].hasRightWall = false;
			if (player != -1)
            	board.playerStatus[player].wallsLeft++;
        }
#endregion
        
        #region Functions checking if accessible (i.e not blocked by a wall)
        bool CanMoveDown(int posX, int posY)
        {
            if (!boardStatus[posX, posY].hasBotWall)
                return true;
            else
                return false;
        }

        bool CanMoveUp(int posX, int posY)
        {
            if (!boardStatus[posX - 1, posY].hasBotWall)
                return true;
            else
                return false;
        }

        
        bool CanMoveRight(int posX, int posY)
        {
            if (!boardStatus[posX, posY].hasRightWall)
                return true;
            else
                return false;
        }

        
        bool CanMoveLeft(int posX, int posY)
        {
            
            if (!boardStatus[posX, posY - 1].hasRightWall)
                return true;
            else
                return false;
        }

        public bool IsPawnMoveLegal(int formerX, int formerY, int newX, int newY)
        {
            int xDiff = newX - formerX;
            int yDiff = newY - formerY;
            
            if (xDiff == 1 && yDiff == 0 &&
                formerX < 8 &&
                !boardStatus[formerX, formerY].hasBotWall &&
                boardStatus[formerX + 1, formerY].isOpen)
            {
                return true;
            }
            
            else if (xDiff == 2 && yDiff == 0 &&
                    formerX < 7 &&
                    !boardStatus[formerX, formerY].hasBotWall &&
                    !boardStatus[formerX + 1, formerY].isOpen &&
                    !boardStatus[formerX + 1, formerY].hasBotWall)
            {
                return true;
            }
            
            else if ((xDiff == 1 && yDiff == -1) &&
                    (formerX < 8 && formerY > 0) &&
                    ((formerX != 7 &&
                    !boardStatus[formerX, formerY].hasBotWall &&   
                     !boardStatus[formerX + 1, formerY].isOpen &&
                    !boardStatus[newX, newY].hasRightWall)
                    ||
                    (formerX != 7 &&                               
                    !boardStatus[formerX, formerY].hasBotWall &&
                     boardStatus[formerX + 1, formerY].hasBotWall &&
                     !boardStatus[formerX + 1, formerY].isOpen &&
                    !boardStatus[newX, newY].hasRightWall)
                    ||
                    (formerY == 1 &&
                    !boardStatus[formerX, formerY - 1].hasRightWall && 
                    !boardStatus[formerX, formerY - 1].isOpen &&
                   !boardStatus[formerX, formerY - 1].hasBotWall)
                    ||
                  (formerY != 1 &&
                  !boardStatus[formerX, formerY - 1].hasRightWall && 
                    boardStatus[formerX, formerY - 2].hasRightWall &&
                   !boardStatus[formerX, formerY - 1].isOpen &&
                   !boardStatus[formerX, formerY - 1].hasBotWall)))
            {
                return true;
            }

           
            else if (xDiff == -1 && yDiff == 0 &&
                    formerX > 0 &&
                    !boardStatus[formerX - 1, formerY].hasBotWall &&
                    boardStatus[formerX - 1, formerY].isOpen)
            {
                return true;
            }
            
            else if (xDiff == -2 && yDiff == 0 &&
                formerX > 1 &&
             !boardStatus[formerX - 1, formerY].hasBotWall &&
             !boardStatus[formerX - 1, formerY].isOpen &&
             !boardStatus[formerX - 2, formerY].hasBotWall)
            {
                return true;
            }
            
            else if (xDiff == -1 && yDiff == 1 &&
                formerX > 0 && formerY < 8 &&
                ((formerX == 1 &&                                
                !boardStatus[formerX - 1, formerY].hasBotWall &&
                !boardStatus[formerX - 1, formerY].isOpen &&
                !boardStatus[formerX - 1, formerY].hasRightWall)
                ||
                (formerX != 1 &&
                !boardStatus[formerX - 1, formerY].hasBotWall && 
                   boardStatus[formerX - 2, formerY].hasBotWall &&
                  !boardStatus[formerX - 1, formerY].isOpen &&
                  !boardStatus[formerX - 1, formerY].hasRightWall)
                  ||
                  (formerY == 7 &&                              
                  !boardStatus[formerX, formerY].hasRightWall &&
                  !boardStatus[formerX, formerY + 1].isOpen &&
                  !boardStatus[formerX - 1, formerY + 1].hasBotWall
                  )
                  ||
                 (formerY != 7 &&
                 !boardStatus[formerX, formerY].hasRightWall && 
                   boardStatus[formerX, formerY + 1].hasRightWall &&
                  !boardStatus[formerX, formerY + 1].isOpen &&
                   !boardStatus[formerX - 1, formerY + 1].hasBotWall)))
            {
                return true;

            }

            else if (xDiff == 0 && yDiff == -1 &&
                    formerY > 0 &&
                    !boardStatus[formerX, formerY - 1].hasRightWall &&
                    boardStatus[formerX, formerY - 1].isOpen)
            {
                return true;
            }
            
            else if (xDiff == 0 && yDiff == -2 &&
                     formerY > 1 &&
                     !boardStatus[formerX, formerY - 1].hasRightWall &&
                     !boardStatus[formerX, formerY - 1].isOpen &&
                     !boardStatus[formerX, formerY - 2].hasRightWall)
            {
                return true;
            }
            
            else if (xDiff == -1 && yDiff == -1 &&
                (formerX > 0 && formerY > 0) &&
                ((formerY == 1 &&                                   
                !boardStatus[formerX, formerY - 1].hasRightWall &&
                !boardStatus[formerX, formerY - 1].isOpen &&
                !boardStatus[newX, newY].hasBotWall)
                ||
                (formerY != 1 &&
                !boardStatus[formerX, formerY - 1].hasRightWall && 
                boardStatus[formerX, formerY - 2].hasRightWall &&
                !boardStatus[formerX, formerY - 1].isOpen &&
                !boardStatus[newX, newY].hasBotWall)
                ||
                (formerX == 1 &&                                    
                !boardStatus[formerX - 1, formerY].hasBotWall &&
                !boardStatus[formerX - 1, formerY].isOpen &&
                !boardStatus[newX, newY].hasRightWall)
                ||
                (formerX != 1 &&
                !boardStatus[formerX - 1, formerY].hasBotWall && 
                boardStatus[formerX - 2, formerY].hasBotWall &&
                !boardStatus[formerX - 1, formerY].isOpen &&
                !boardStatus[newX, newY].hasRightWall)))
            {
                return true;

            }

            else if (xDiff == 0 && yDiff == 1 &&
                formerY < 8 &&
                !boardStatus[formerX, formerY].hasRightWall &&
                boardStatus[formerX, formerY + 1].isOpen)
            {
                return true;
            }
            
            else if (xDiff == 0 && yDiff == 2 &&
                formerY < 7 &&
                !boardStatus[formerX, formerY].hasRightWall &&
                !boardStatus[formerX, formerY + 1].isOpen &&
                 !boardStatus[formerX, formerY + 1].hasRightWall)
            {
                return true;
            }
            
            else if (xDiff == 1 && yDiff == 1 &&
                (formerX < 8 && formerY < 8) &&
             ((formerY == 7 &&         
             !boardStatus[formerX, formerY].hasRightWall &&
             !boardStatus[formerX, formerY + 1].isOpen &&
             !boardStatus[formerX, formerY + 1].hasBotWall)
             ||
             (formerY != 7 &&
             !boardStatus[formerX, formerY].hasRightWall &&
                boardStatus[formerX, formerY + 1].hasRightWall &&
               !boardStatus[formerX, formerY + 1].isOpen &&
               !boardStatus[formerX, formerY + 1].hasBotWall)
               ||
               (formerX == 7 &&                            
               !boardStatus[formerX, formerY].hasBotWall &&
               !boardStatus[formerX + 1, formerY].isOpen &&
               !boardStatus[formerX + 1, formerY].hasRightWall)
               ||
              (!boardStatus[formerX, formerY].hasBotWall && 
                boardStatus[formerX + 1, formerY].hasBotWall &&
               !boardStatus[formerX + 1, formerY].isOpen &&
               !boardStatus[formerX + 1, formerY].hasRightWall)))
            {
                return true;
            }
            return false;
        }

        public bool IsPawnMoveLegalSimplified(int formerX, int formerY, int newX, int newY)
        {
            int diffX = newX - formerX;
            int diffY = newY - formerY;
            if (diffX == 1 && diffY == 0)
                return CanMoveDown(formerX, formerY);
            else if (diffX == -1 && diffY == 0)
                return CanMoveUp(formerX, formerY);
            else if (diffX == 0 && diffY == -1)
                return CanMoveLeft(formerX, formerY);
            else if (diffX == 0 && diffY == 1)
                return CanMoveRight(formerX, formerY);
            else
                Debug.LogError("We are checking a move that should never be called with IsPawnMoveLegalSimplified function");
                return false; 
        }
        #endregion
    }
}
