using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SuperTicTacToe
{

    enum protocol
    {
        login, // login:username to register for a game
        move,  // move:superRow:superCol:row:col to make a move - indicates enemy move for the other player
        game,  // game:yourturn to notify the player that it's their turn game:wait to notify the player that they are waiting for their turn game:over:WINNER
        error, // protocol error
    }


    class CustomServer : BaseServer
    {
        private List<TcpClient> clients = new List<TcpClient>();
        private List<onlineGame> games = new List<onlineGame>();
        public CustomServer(int port) : base(port) { }


        public override void OnMessageReceived(TcpClient client, string message)
        {
            protocol firstPart = protocol.error;

            String[] messageParts = message.Split(":");
            switch(messageParts[0])
            {
                case "login":
                    firstPart = protocol.login;
                    break;
                case "move":
                    firstPart = protocol.move;
                    break;
                case "game":
                    firstPart = protocol.game;
                    break;
            }
                switch (firstPart)
                {
                    case protocol.login:
                        Send(client, "login:ok");
                    // check if the client is already in a game
                    clients.Append(client);
                        if (clients.Count >= 2)
                        {
                            games.Add(new onlineGame(clients[0], clients[1], GameMode.OfficialRules, this));
                        }
                        break;

                    case protocol.move:
                        onlineGame game = null;
                        // check the games list if the client is in a game
                        for (int i = 0; i < games.Count; i++)
                        {
                            if (games[i].Client1 == client || games[i].Client2 == client)
                            {
                                // found the game
                                game = games[i];
                                break;
                            }
                        }
                        if (game != null)
                        {

                            // parse the move
                            int superRow = int.Parse(messageParts[1]);
                            int superCol = int.Parse(messageParts[2]);
                            int row = int.Parse(messageParts[3]);
                            int col = int.Parse(messageParts[4]);
                            // make the move
                            game.makeMove(client, superRow, superCol, row, col);
                        }
                        else
                        {
                            Send(client, "error:not in a game");
                        }

                        break;

                }
            }
            

        }
    }

