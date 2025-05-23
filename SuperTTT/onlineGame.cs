using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SuperTicTacToe
{

    class main
    {
        static void Main(string[] args)
        {
            // Initialize the server and start listening for connections
            CustomServer server = new CustomServer(80);
            server.Start();
        }
    }

    class onlineGame
    {
        private CustomServer _server;
        private TcpClient _client1;
        private TcpClient _client2;
        private GameMode _gameMode;
        private Game _game;
        private Player _currentPlayer;


        public onlineGame(TcpClient pClient1, TcpClient pClient2, GameMode pGameMode, CustomServer pServer)
        {
            _server = pServer;
            _client1 = pClient1; // Player X
            _client2 = pClient2; // Player O 
            _gameMode = pGameMode;
            _game = new Game(_gameMode);
            _currentPlayer = _game.GetCurrentPlayer(); // Start with player X
            _server.Send(_client1, "game:yourturn"); // Notify player X
            _server.Send(_client2, "game:wait"); // Notify player O
        }

        public TcpClient Client1
        {
            get { return _client1; }
        }

        public TcpClient Client2
        {
            get { return _client2; }
        }

        public void makeMove(TcpClient pClient, int superRow, int superCol, int row, int col)
        {

            if (_currentPlayer == Player.X)
            {
                if (pClient == _client1)
                {
                    try
                    {
                        _game.MakeMove(superRow, superCol, row, col);

                        // Check for game over conditions
                        if (_game.OverallWinner != Player.None)
                        {
                            _server.Send(_client1, $"game:over:WINNER:{_game.OverallWinner}");
                            _server.Send(_client2, $"game:over:WINNER:{_game.OverallWinner}");
                            return;
                        }
                        if (_game.IsGameDraw())
                        {
                            _server.Send(_client1, "game:over:DRAW");
                            _server.Send(_client2, "game:over:DRAW");
                            return;
                        }

                        // Game continues, switch player and notify
                        _currentPlayer = Player.O;
                        _server.Send(_client2, $"move:{superRow}:{superCol}:{row}:{col}");
                        if (_game.GetNextSuperRow() == -1)
                        {
                            _server.Send(_client2, "game:yourturn:any");
                        }
                        else
                        {
                            _server.Send(_client2, $"game:yourturn:{_game.GetNextSuperRow()}:{_game.GetNextSuperCol()}");
                        }
                        _server.Send(_client1, "game:wait");
                    }
                    catch (Exception ex)
                    {
                        _server.Send(pClient, $"error:{ex.Message}");
                    }
                }
                else
                {
                    _server.Send(pClient, "error:invalid player");
                }
            }
            else // Current player is O
            {
                if (pClient == _client2)
                {
                    try
                    {
                        _game.MakeMove(superRow, superCol, row, col);

                        // Check for game over conditions
                        if (_game.OverallWinner != Player.None)
                        {
                            _server.Send(_client1, $"game:over:WINNER:{_game.OverallWinner}");
                            _server.Send(_client2, $"game:over:WINNER:{_game.OverallWinner}");
                            return;
                        }
                        if (_game.IsGameDraw())
                        {
                            _server.Send(_client1, "game:over:DRAW");
                            _server.Send(_client2, "game:over:DRAW");
                            return;
                        }

                        // Game continues, switch player and notify
                        _currentPlayer = Player.X; // Corrected: Switch to player X
                        _server.Send(_client1, $"move:{superRow}:{superCol}:{row}:{col}");
                        if (_game.GetNextSuperRow() == -1)
                        {
                            _server.Send(_client1, "game:yourturn:any");
                        }
                        else
                        {
                            _server.Send(_client1, $"game:yourturn:{_game.GetNextSuperRow()}:{_game.GetNextSuperCol()}");
                        }
                        _server.Send(_client2, "game:wait");
                    }
                    catch (Exception ex)
                    {
                        _server.Send(pClient, $"error:{ex.Message}");
                    }
                }
                else
                {
                    _server.Send(pClient, "error:invalid player");
                }
            }
        }
    }
}
