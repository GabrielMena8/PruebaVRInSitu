
using WebSocketSharp.Server;
class InsituChatServer
{
    static void Main(string[] args)
    {
        // Crear una instancia del servidor WebSocket en la dirección y puerto especificados
        WebSocketServer wssv = new WebSocketServer("ws://127.0.0.1:8080");

        // Añadir el servicio de WebSocket para la ruta "/chat"
        wssv.AddWebSocketService<ChatRoom>("/chat");

        bool running = true;

        // Bucle principal para manejar los comandos del usuario
        while (running)
        {
            Console.WriteLine("Escribe 'start' para iniciar el servidor, 'stop' para detenerlo, 'exit' para salir:");
            string command = Console.ReadLine().ToLower();

            switch (command)
            {
                case "start":
                    // Iniciar el servidor si no está ya en ejecución
                    if (!wssv.IsListening)
                    {
                        wssv.Start();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Servidor WebSocket iniciado.");
                        Console.ResetColor();
                    }
                    break;

                case "stop":
                    // Detener el servidor si está en ejecución
                    if (wssv.IsListening)
                    {
                        wssv.Stop();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Servidor detenido.");
                        Console.ResetColor();
                    }
                    break;

                case "exit":
                    // Detener el servidor si está en ejecución y salir del bucle
                    if (wssv.IsListening)
                    {
                        wssv.Stop();
                    }
                    running = false;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Saliendo...");
                    Console.ResetColor();
                    break;

                default:
                    // Manejar comandos no reconocidos
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Comando no reconocido.");
                    Console.ResetColor();
                    break;
            }
        }
    }
}
