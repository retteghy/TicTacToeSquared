﻿using System;
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
            Console.WriteLine(message);

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
                        clients.Add(client);
                        Console.WriteLine($"Client logged in clients in list: {clients.Count}");
                        if (clients.Count >= 2)
                            {
                                Console.WriteLine("Starting game");
                                TcpClient client1 = clients[0];
                                TcpClient client2 = clients[1];
                                games.Add(new onlineGame(client1, client2, GameMode.OfficialRules, this));
                                clients.RemoveRange(0, 2); // Remove the first two clients
                                Console.WriteLine($"Game started. Clients remaining in queue: {clients.Count}");
                    }
                    break;

                    case protocol.move:
                        onlineGame game = null;
                    // check the games list if the client is in a game
                    Console.WriteLine(client.Client.RemoteEndPoint);
                        for (int i = 0; i < games.Count; i++)
                        {
                            Console.WriteLine(games[i].Client1.Client.RemoteEndPoint);
                            if (games[i].Client1.Client.RemoteEndPoint == client.Client.RemoteEndPoint || games[i].Client2.Client.RemoteEndPoint == client.Client.RemoteEndPoint)
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
                            Console.WriteLine("Player was not found in Game Pool!");
                            Send(client, "error:not in a game");
                        }

                        break;

                }
            }
            

        }
    }

