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
    startgame // Command to initiate a game from the lobby
    }


    class CustomServer : BaseServer
    {
        private List<TcpClient> waitingClients = new List<TcpClient>(); // Renamed from clients
        private List<onlineGame> games = new List<onlineGame>();
        public CustomServer(int port) : base(port) 
        {
            waitingClients = new List<TcpClient>(); // Initialize the renamed list
        }


        public override void OnMessageReceived(TcpClient client, string message)
        {
            String[] messageParts = message.Split(':');
            protocol parsedCommand = protocol.error; // Default to error

            if (messageParts.Length > 0)
            {
                switch (messageParts[0].ToLower()) // Use ToLower() for case-insensitivity
                {
                    case "login":
                        parsedCommand = protocol.login;
                        break;
                    case "move":
                        parsedCommand = protocol.move;
                        break;
                    case "game": // "game" is mostly server-to-client. Consider if client should send it.
                                 // If client is not supposed to send "game", it will fall through to default.
                        parsedCommand = protocol.game; 
                        break;
                    case "startgame":
                        parsedCommand = protocol.startgame;
                        break;
                    // No "error" case here; it's handled by the default in the next switch.
                }
            }

            switch (parsedCommand)
            {
                case protocol.login:
                        // Check if client is already in waitingClients or in a game to prevent duplicate logins
                        if (waitingClients.Contains(client) || games.Any(g => g.Client1 == client || g.Client2 == client))
                        {
                            Send(client, "error:already_logged_in_or_in_game");
                            return;
                        }
                        waitingClients.Add(client);
                        Send(client, "login:ok");
                        Send(client, $"lobby:info:{waitingClients.Count}");
                        Console.WriteLine($"Client {client.GetHashCode()} logged in. Clients in lobby: {waitingClients.Count}");
                        // Removed automatic game start and games.Clear()
                        break;

                    case protocol.startgame:
                        if (!waitingClients.Contains(client))
                        {
                            Send(client, "error:not_in_lobby");
                            return;
                        }

                        if (waitingClients.Count >= 2)
                        {
                            TcpClient player1 = client;
                            TcpClient player2 = waitingClients.FirstOrDefault(c => c != player1);

                            if (player2 != null)
                            {
                                onlineGame newGame = new onlineGame(player1, player2, GameMode.OfficialRules, this);
                                games.Add(newGame);
                                waitingClients.Remove(player1);
                                waitingClients.Remove(player2);
                                Console.WriteLine($"Game started between {player1.GetHashCode()} and {player2.GetHashCode()}. Games active: {games.Count}. Clients in lobby: {waitingClients.Count}");
                                // onlineGame constructor handles initial turn notifications
                            }
                            else
                            {
                                // This case should ideally not be reached if waitingClients.Count >= 2 and client is in waitingClients
                                Send(client, "error:internal_server_error:could_not_find_opponent");
                                Console.WriteLine($"Error: Could not find opponent for {client.GetHashCode()} even though lobby count was {waitingClients.Count}");
                            }
                        }
                        else
                        {
                            Send(client, $"lobby:wait:Not enough players. Currently {waitingClients.Count} in lobby.");
                        }
                        break;

                    case protocol.move:
                        // Refined game finding using LINQ
                        onlineGame game = games.FirstOrDefault(g => g.Client1 == client || g.Client2 == client);
                        
                        if (game != null)
                        {
                            // Ensure messageParts has enough elements for a move command
                            if (messageParts.Length >= 5)
                            {
                                try
                                {
                                    int superRow = int.Parse(messageParts[1]);
                                    int superCol = int.Parse(messageParts[2]);
                                    int row = int.Parse(messageParts[3]);
                                    int col = int.Parse(messageParts[4]);
                                    game.makeMove(client, superRow, superCol, row, col);
                                }
                                catch (FormatException ex)
                                {
                                    Send(client, "error:invalid_move_format");
                                    Console.WriteLine($"Invalid move format from client {client.GetHashCode()}: {message}. Error: {ex.Message}");
                                }
                                catch (ArgumentOutOfRangeException ex) // Catch potential errors from int.Parse if parts are not numbers
                                {
                                    Send(client, "error:invalid_move_values");
                                    Console.WriteLine($"Invalid move values from client {client.GetHashCode()}: {message}. Error: {ex.Message}");
                                }
                            }
                            else
                            {
                                Send(client, "error:incomplete_move_command");
                                Console.WriteLine($"Incomplete move command from client {client.GetHashCode()}: {message}");
                            }
                        }
                        else
                        {
                            Send(client, "error:not_in_a_game");
                        }
                        break;
                    
                    case protocol.game: // Example: If client sends "game" command, treat as error or handle if intended
                        Send(client, "error:game_command_not_expected_from_client");
                        Console.WriteLine($"Client {client.GetHashCode()} sent unexpected 'game' command: {message}");
                        break;

                    default: // Handles protocol.error or any other unhandled valid enum values
                        Send(client, "error:unknown_or_invalid_command");
                        Console.WriteLine($"Unknown or invalid command received from client {client.GetHashCode()}: {message}");
                        break;
                }
            }
        }

        protected override void OnClientDisconnected(TcpClient client)
        {
            Console.WriteLine($"CustomServer: Client disconnected (Address: {client?.Client?.RemoteEndPoint?.ToString() ?? "N/A"})");

            // Remove from waiting list
            bool removedFromWaiting = waitingClients.Remove(client);
            if (removedFromWaiting)
            {
                Console.WriteLine("Client removed from waiting list.");
            }

            // Check if the client was in any active game
            onlineGame gameInstance = games.FirstOrDefault(g => g.Client1 == client || g.Client2 == client);
            if (gameInstance != null)
            {
                Console.WriteLine("Client was in an active game. Notifying opponent and removing game.");
                TcpClient opponent = (gameInstance.Client1 == client) ? gameInstance.Client2 : gameInstance.Client1;
                if (opponent != null && opponent.Connected)
                {
                    try
                    {
                        // Ensure BaseServer's Send method is accessible or CustomServer has its own
                        Send(opponent, "game:opponent_disconnected");
                        Send(opponent, "game:over:FORFEIT"); // Ensure this is sent
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error notifying opponent: {ex.Message}");
                    }
                }
                games.Remove(gameInstance); // Remove the game
            }
            
            // Optional: If you maintain an allConnectedClients list
            // allConnectedClients.Remove(client);

            base.OnClientDisconnected(client); // Call base method if it has any logic
        }
    }

