using System;
using WebSocketSharp;
using WebSocketSharp.Server;

class InsituChatServer
{
    static void Main(string[] args)
    {
        // REMINDER:
        // Para ver la IP de tu máquina:
        //   - En Windows, abre el "Símbolo del sistema" y escribe: ipconfig
        //       Busca la línea "IPv4 Address" en la sección correspondiente a tu adaptador de red.
        //   - En macOS o Linux, abre la Terminal y escribe: ifconfig (o ip addr)
        //       Busca la dirección asociada a tu interfaz activa (por ejemplo, "en0" o "wlan0").
        //
        // Puedes usar esta IP para que otros dispositivos se conecten al servidor.
        // Además, si deseas que el servidor escuche en todas las interfaces (modo "external"),
        // se puede usar la dirección "0.0.0.0".

        Console.WriteLine("Selecciona el modo de conexión:");
        Console.WriteLine("Escribe 'external' para usar 0.0.0.0 (escucha en todas las interfaces, permite conexiones externas)");
        Console.WriteLine("O escribe 'internal' para usar 127.0.0.1 (solo conexiones locales en la misma máquina)");
        string mode = Console.ReadLine().ToLower();

        string ip = "127.0.0.1";
        if (mode == "external")
        {
            ip = "0.0.0.0"; // Escucha en todas las interfaces
        }
        string url = $"ws://{ip}:8080";

        // Crear el servidor WebSocket en la URL determinada
        WebSocketServer wssv = new WebSocketServer(url);
        wssv.AddWebSocketService<ChatRoom>("/chat");

        Console.WriteLine($"Servidor configurado en {url}/chat");
        Console.WriteLine("------------------------------------------------------");
        Console.WriteLine("Recordatorio: Para saber la IP de tu máquina, usa 'ipconfig' (Windows) o 'ifconfig'/'ip addr' (Linux/macOS).");
        Console.WriteLine("Si usas 'external', el servidor escuchará en todas las interfaces y podrás usar la IP real de tu máquina para conectarte desde otros dispositivos.");
        Console.WriteLine("------------------------------------------------------");

        bool running = true;
        Console.WriteLine("Comandos disponibles:");
        Console.WriteLine("  start  -> Iniciar el servidor");
        Console.WriteLine("  stop   -> Detener el servidor");
        Console.WriteLine("  setip  -> Cambiar modo de conexión (internal / external)");
        Console.WriteLine("  exit   -> Salir");

        // Bucle principal para manejar comandos en la consola
        while (running)
        {
            Console.Write("> ");
            string command = Console.ReadLine().ToLower();
            switch (command)
            {
                case "start":
                    if (!wssv.IsListening)
                    {
                        wssv.Start();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Servidor WebSocket iniciado.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("El servidor ya está en ejecución.");
                    }
                    break;

                case "stop":
                    if (wssv.IsListening)
                    {
                        wssv.Stop();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Servidor detenido.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine("El servidor ya está detenido.");
                    }
                    break;

                case "setip":
                    // Cambiar el modo de conexión en tiempo de ejecución
                    if (wssv.IsListening)
                    {
                        wssv.Stop();
                    }
                    Console.WriteLine("Ingresa el modo deseado ('external' o 'internal'):");
                    string newMode = Console.ReadLine().ToLower();
                    if (newMode == "external")
                    {
                        ip = "0.0.0.0";
                    }
                    else
                    {
                        ip = "127.0.0.1";
                    }
                    url = $"ws://{ip}:8080";
                    wssv = new WebSocketServer(url);
                    wssv.AddWebSocketService<ChatRoom>("/chat");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Modo cambiado. Nuevo servidor configurado en {url}/chat");
                    Console.ResetColor();
                    break;

                case "exit":
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
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Comando no reconocido.");
                    Console.ResetColor();
                    break;
            }
        }
    }
}
