
# InsituChatApp

**InsituChatApp** es una aplicación de chat en tiempo real basada en WebSockets para comunicación entre un servidor y múltiples clientes. El sistema permite la autenticación de usuarios, la gestión de salas de chat, el envío de mensajes, transferencia de objetos 3D y archivos, además de reconexión automática en caso de fallo de conexión.

El proyecto está compuesto por dos partes principales:  

- **Servidor:** Implementado en C# utilizando **WebSocketSharp** y **Newtonsoft.Json** para la gestión de usuarios y salas de chat.  
- **Cliente:** Desarrollado en **Unity** con manejo de mensajes, objetos 3D y archivos desde el cliente receptor.

---

## 📜 Resumen General del Proyecto

- **Autenticación y Gestión de Salas:**  
  Los usuarios pueden iniciar sesión (roles: "admin" y "user"), crear, eliminar o unirse a salas de chat. El servidor mantiene una lista de salas y usuarios conectados, enviando notificaciones a los clientes en tiempo real.

- **Mensajería y Estado del Usuario:**  
  Los clientes envían mensajes y actualizan el estado del usuario (activo, escribiendo, inactivo). Notificaciones importantes, como la conexión y desconexión de usuarios, se envían a todos los clientes en la misma sala.

- **Transferencia de Objetos y Archivos:**  
  - **Objetos 3D:** Serialización y deserialización de objetos Unity (Vector3, Quaternion, Color) en JSON para su envío y recreación en el cliente receptor.  
  - **Archivos:** Los archivos se dividen en fragmentos (aproximadamente 200 KB por fragmento) para su transmisión. Por ahora, **los archivos solo se guardan correctamente cuando el cliente receptor es el mismo laptop donde corre el servidor**, debido a que el guardado dinámico en otras PCs aún no está implementado.

- **Reconexión Automática:**  
  Tanto el cliente como el servidor intentan reconectar automáticamente en caso de fallo, con delays de 5 segundos entre cada intento.

---

## ⚠️ Bugs y Ajustes Conocidos

- **DLL de WebSocketSharp Modificada:** Se modificó la DLL original para funcionar correctamente en este entorno.  
- **Limitación de Buffer:** Los fragmentos de archivo tienen un tamaño máximo de 200 KB, lo que puede causar problemas al intentar enviar archivos grandes.  
- **Guardado de Archivos:** Los archivos solo se guardan correctamente en la PC que ejecuta el servidor. La lógica para el guardado dinámico en otros clientes aún no ha sido implementada.  
- **Modo Desarrollo:** El servidor se ejecuta en modo desarrollo (.exe) debido a errores no manejados en la conexión **WSSV** en modo release.

---

## 🛠️ Pasos para Ejecutar el Proyecto

### 🔹 Servidor

1. **Requisitos:**
   - .NET Framework o .NET Core.  
   - DLLs de WebSocketSharp (modificada) y Newtonsoft.Json.

2. **Pasos:**
   - Abre la solución del servidor en **Visual Studio**.  
   - Compila el proyecto.  
   - Ejecuta el .exe generado en **modo desarrollo**.  

3. **Verificación:**  
   La consola del servidor debe mostrar logs sobre conexiones, autenticación, creación de salas, mensajes y recepción de archivos.

---

### 🔹 Cliente (Unity)

1. **Requisitos:**
   - Unity (versión 2018 o superior).  
   - Todos los scripts deben estar incluidos en la estructura del proyecto Unity.

2. **Pasos:**
   - Abre el proyecto en Unity.  
   - Asegúrate de tener el script **AuthManager** en la escena principal.  
   - Configura la URL del servidor a través del panel de login.  
   - Ejecuta la escena desde Unity o crea un build del cliente.  
   - Realiza el login, únete a una sala y prueba las funciones de chat, envío de objetos y archivos.

3. **Notas sobre Archivos:**  
   - Los archivos recibidos se guardarán en la carpeta `Downloads/InsituChatApp` de la PC receptora.  
   - Dado que el guardado dinámico no está implementado, **solo se almacenan correctamente si el cliente receptor es el laptop que ejecuta el servidor**.

---

## 📂 Estructura del Código

- **AuthManager.cs:** Gestiona la conexión al servidor, la autenticación y la reconexión automática. También maneja el guardado de archivos.  
- **ChatClient.cs:** Controla la lógica del chat y la interacción con objetos 3D.  
- **ChatRoom.cs:** (Servidor) Gestiona la autenticación, salas de chat y mensajes. También reensambla fragmentos de archivos.  
- **ObjectManager.cs:** Instancia objetos 3D recibidos a través de la red en el cliente.  
- **Conversores JSON:** Permiten serializar tipos de Unity como Vector3, Quaternion y Color en JSON.

---

## 📊 Estadísticas y Comandos Disponibles

- El servidor ofrece estadísticas básicas, como mensajes enviados/recibidos, latencia promedio y tiempo de actividad.  
- **Comandos:**  
  - `LOGIN <usuario> <contraseña>` — Iniciar sesión.  
  - `CREATE_ROOM <nombre>` — Crear sala (solo admin).  
  - `JOIN_ROOM <nombre>` — Unirse a una sala.  
  - `VIEW_CONNECTED` — Ver usuarios conectados.  
  - `SEND_OBJECT <usuario> <data>` — Enviar objeto 3D.  
  - `SEND_FILE_USER <usuario> <archivo>` — Enviar archivo a usuario.  

---

## 🧩 Próximos Pasos

- Implementar guardado dinámico de archivos en la carpeta de descargas del cliente receptor.  
- Resolver errores relacionados con el modo release del servidor.  
- Ampliar la lógica de transferencia para soportar archivos más grandes y gestionar fragmentos de manera más eficiente.

---

## 📄 Licencia

[MIT License] (o la que prefieras incluir aquí).
