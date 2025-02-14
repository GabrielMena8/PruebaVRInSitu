using System;
using System.Net;
using WebSocketSharp;
using WebSocketSharp.Server;

class InsituChatServer
{
    // Método para obtener la IP local (IPv4) que no sea de loopback
    static string GetLocalIPAddress()
    {
        string localIP = "127.0.0.1";
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                // Solo direcciones IPv4
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(ip))
                {
                    localIP = ip.ToString();
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al obtener la IP local: " + ex.Message);
        }
        return localIP;
    }

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
        // se puede usar la dirección "0.0.0.0" en el binding, pero para mostrar la IP real usaremos GetLocalIPAddress().

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
        Console.WriteLine("------------------------------------------------------");

        // Si el modo es external, mostramos la IP real de la máquina para que otros equipos se conecten.
        if (mode == "external")
        {
            string localIP = GetLocalIPAddress();
            Console.WriteLine($"La IP de esta máquina es: {localIP}");
            Console.WriteLine($"Para conectarte desde otra PC, usa: ws://{localIP}:8080/chat");
        }

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
                        // Si estamos en modo external, mostramos la IP real en el mensaje
                        if (mode == "external")
                        {
                            string localIP = GetLocalIPAddress();
                            Console.WriteLine($"Servidor iniciado en: ws://{localIP}:8080/chat");
                        }
                        else
                        {
                            Console.WriteLine($"Servidor iniciado en: {url}/chat");
                        }
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
                    if (wssv.IsListening)
                    {
                        wssv.Stop();
                    }
                    Console.WriteLine("Ingresa el modo deseado ('external' o 'internal'):");
                    string newMode = Console.ReadLine().ToLower();
                    if (newMode == "external")
                    {
                        ip = "0.0.0.0";
                        mode = "external";
                    }
                    else
                    {
                        ip = "127.0.0.1";
                        mode = "internal";
                    }


                    url = $"ws://{ip}:8080";
                    wssv = new WebSocketServer(url);
                    wssv.AddWebSocketService<ChatRoom>("/chat");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Modo cambiado. Nuevo servidor configurado en {url}/chat");
                    if (mode == "external")
                    {
                        string localIP = GetLocalIPAddress();
                        Console.WriteLine($"La IP de esta máquina es: {localIP}");
                        Console.WriteLine($"Para conectarte desde otra PC, usa: ws://{localIP}:8080/chat");
                    }
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
