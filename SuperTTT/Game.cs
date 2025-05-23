
using System; 

namespace SuperTicTacToe
{
    public enum Player
    {
        None,
        X,
        O
    }

    public enum GameMode
    {
        OfficialRules,
        AlternativeRules
    }

    public class Game
    {
        private Player[,] _boards; // Super board initialization
        private Player[,] _superBoard; // Super board status
        private Player _currentPlayer; // Current player
        private int _nextSuperRow; // Next super board row
        private int _nextSuperCol; // Next super board column
        private GameMode _gameMode; // Game mode (official or alternative)

        public Game(GameMode gameMode)
        {
            _boards = new Player[9, 9]; // 9x9 board for super board
            _superBoard = new Player[3, 3]; // 3x3 board for super board status
            _currentPlayer = Player.X; // Start with player X
            _nextSuperRow = -1; // Indicates any super board can be played on the first move
            _nextSuperCol = -1; // Indicates any super board can be played on the first move
            _gameMode = gameMode;
            OverallWinner = Player.None; // Initialize OverallWinner
        }

        public Player OverallWinner { get; private set; } // Added OverallWinner property

        public Player GetCurrentPlayer()
        {
            return _currentPlayer;
        }


        public bool IsSubBoardFull(int superRow, int superCol)
        {
            if (superRow < 0 || superRow >= 3 || superCol < 0 || superCol >= 3)
            {
                return false;
            }

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (_boards[superRow * 3 + row, superCol * 3 + col] == Player.None) 
                    {
                        return false; // If any square is empty, the sub-board is not full
                    }
                }
            }
            return true;
        }

        public Player GetSuperBoardStatus(int superRow, int superCol) // Check Winner of sub-board board[0-2,0-2]
        {
            if (superRow < 0 || superRow >= 3 || superCol < 0 || superCol >= 3)
            {
                return Player.None;
            }

            return _superBoard[superRow, superCol];
        }

        public bool MakeMove(int superRow, int superCol, int row, int col)
        {
            if (_nextSuperRow != -1 && (superRow != _nextSuperRow || superCol != _nextSuperCol)) // Check if the move is on correct sub-board
            {
                throw new ArgumentException("You must play in the designated super board.");
            }

            if (superRow < 0 || superRow >= 3 || superCol < 0 || superCol >= 3) // Check if the super board position is valid
            {
                throw new ArgumentException("Invalid super board position");
            }

            if (row < 0 || row >= 3 || col < 0 || col >= 3) // Check sub-board position
            {
                throw new ArgumentException("Invalid board position");
            }

            if (_superBoard[superRow, superCol] != Player.None && (_gameMode == GameMode.OfficialRules || !IsSubBoardFull(superRow, superCol))) // Check if sub-board is full or won in official rules
            {
                throw new ArgumentException("Super board position is already occupied");
            }

            if (_boards[superRow * 3 + row, superCol * 3 + col] != Player.None) // Check if the position is already occupied
            {
                throw new ArgumentException("Board position is already occupied");
            }

            _boards[superRow * 3 + row, superCol * 3 + col] = _currentPlayer; // Place the current player's mark on the board

            // Check if the current player has won the super board
            if (CheckSuperBoardWin(superRow, superCol))
            {
                _superBoard[superRow, superCol] = _currentPlayer;
            }

            // Determine the target sub-board for the next move
            int targetNextSuperRow = row;
            int targetNextSuperCol = col;

            // Check if the target sub-board is already won or full
            if (_superBoard[targetNextSuperRow, targetNextSuperCol] != Player.None || IsSubBoardFull(targetNextSuperRow, targetNextSuperCol))
            {
                // If won or full, the next player can play anywhere
                _nextSuperRow = -1;
                _nextSuperCol = -1;
            }
            else
            {
                // Otherwise, the next move is in the target sub-board
                _nextSuperRow = targetNextSuperRow;
                _nextSuperCol = targetNextSuperCol;
            }

            // Check for overall winner or draw
            if (CheckOverallWinner() || IsGameDraw())
            {
                // Game over logic can be handled here or by the caller
            }
            else
            {
                _currentPlayer = _currentPlayer == Player.X ? Player.O : Player.X; // Switch players if game is not over
            }

            return true;
        }

        public bool CheckSuperBoardWin(int superRow, int superCol)
        {
            // Check rows
            for (int i = 0; i < 3; i++)
            {
                if (_boards[superRow * 3 + i, superCol * 3] == _boards[superRow * 3 + i, superCol * 3 + 1] &&
                    _boards[superRow * 3 + i, superCol * 3 + 1] == _boards[superRow * 3 + i, superCol * 3 + 2] &&
                    _boards[superRow * 3 + i, superCol * 3] != Player.None)
                {
                    return true;
                }
            }

            // Check columns
            for (int i = 0; i < 3; i++)
            {
                if (_boards[superRow * 3, superCol * 3 + i] == _boards[superRow * 3 + 1, superCol * 3 + i] &&
                    _boards[superRow * 3 + 1, superCol * 3 + i] == _boards[superRow * 3 + 2, superCol * 3 + i] &&
                    _boards[superRow * 3, superCol * 3 + i] != Player.None)
                {
                    return true;
                }
            }

            // Check diagonals
            if (_boards[superRow * 3, superCol * 3] == _boards[superRow * 3 + 1, superCol * 3 + 1] &&
                _boards[superRow * 3 + 1, superCol * 3 + 1] == _boards[superRow * 3 + 2, superCol * 3 + 2] &&
                _boards[superRow * 3, superCol * 3] != Player.None)
            {
                return true;
            }

            if (_boards[superRow * 3, superCol * 3 + 2] == _boards[superRow * 3 + 1, superCol * 3 + 1] &&
                _boards[superRow * 3 + 1, superCol * 3 + 1] == _boards[superRow * 3 + 2, superCol * 3] &&
                _boards[superRow * 3, superCol * 3 + 2] != Player.None)
            {
                return true;
            }

            return false;
        }

        public bool CheckOverallWinner() // Added CheckOverallWinner method
        {
            // Check rows
            for (int i = 0; i < 3; i++)
            {
                if (_superBoard[i, 0] == _superBoard[i, 1] &&
                    _superBoard[i, 1] == _superBoard[i, 2] &&
                    _superBoard[i, 0] != Player.None)
                {
                    OverallWinner = _superBoard[i, 0];
                    return true;
                }
            }

            // Check columns
            for (int i = 0; i < 3; i++)
            {
                if (_superBoard[0, i] == _superBoard[1, i] &&
                    _superBoard[1, i] == _superBoard[2, i] &&
                    _superBoard[0, i] != Player.None)
                {
                    OverallWinner = _superBoard[0, i];
                    return true;
                }
            }

            // Check diagonals
            if (_superBoard[0, 0] == _superBoard[1, 1] &&
                _superBoard[1, 1] == _superBoard[2, 2] &&
                _superBoard[0, 0] != Player.None)
            {
                OverallWinner = _superBoard[0, 0];
                return true;
            }

            if (_superBoard[0, 2] == _superBoard[1, 1] &&
                _superBoard[1, 1] == _superBoard[2, 0] &&
                _superBoard[0, 2] != Player.None)
            {
                OverallWinner = _superBoard[0, 2];
                return true;
            }

            return false;
        }

        public bool IsGameDraw() // Added IsGameDraw method
        {
            if (OverallWinner != Player.None)
            {
                return false; // Not a draw if there's a winner
            }

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (_superBoard[r, c] == Player.None && !IsSubBoardFull(r, c))
                    {
                        return false; // If any sub-board is not won and not full, the game is not a draw
                    }
                }
            }

            return true; // All sub-boards are either won or full, and there's no overall winner
        }

        public int GetNextSuperRow() { return _nextSuperRow; } // Added getter for _nextSuperRow

        public int GetNextSuperCol() { return _nextSuperCol; } // Added getter for _nextSuperCol

    }
}