# Mobile Remote Toolkit Agent

El **Agent** es el componente que se ejecuta de forma nativa en máquinas que tienen dispositivos Android conectados por USB. Se comunica con la API central para ejecutar comandos remotos.

## ¿Por qué un Agent?

Los contenedores Docker/Kubernetes **NO** pueden acceder directamente a dispositivos USB del host. El agent resuelve esto ejecutándose nativamente y comunicándose con la API contenerizada.

## Instalación

### Windows

```powershell
# Descargar el agent
# Extraer archivos en C:\Program Files\Mobile Toolkit Agent\

# Instalar como servicio de Windows
.\install-agent.ps1 -Action install -ApiBaseUrl "https://your-api.company.com"

# Iniciar el servicio
.\install-agent.ps1 -Action start

# Verificar estado
.\install-agent.ps1 -Action status
```

### Linux

```bash
# Descargar el agent
# Extraer archivos en /opt/mobile-toolkit-agent/

# Instalar como systemd service
sudo API_BASE_URL="https://your-api.company.com" ./install-agent.sh install

# Iniciar el servicio
sudo ./install-agent.sh start

# Verificar estado
./install-agent.sh status
```

## Configuración

### appsettings.json

```json
{
  "ApiBaseUrl": "https://your-api.company.com",
  "AgentId": "agent-hostname",
  "RefreshInterval": "00:00:30",
  "AndroidTools": {
    "AdbPath": "/usr/local/bin/adb",
    "AutoDetectTools": true
  },
  "Security": {
    "ApiKey": "your-api-key-here"
  }
}
```

### Variables de Entorno

- `API_BASE_URL`: URL de la API central
- `AGENT_ID`: Identificador único del agent
- `API_KEY`: Clave de API para autenticación

## Requisitos

### Windows

- .NET 8.0 Runtime
- Android SDK Platform Tools (ADB)
- Privilegios de administrador (para instalación)

### Linux

- .NET 8.0 Runtime
- Android SDK Platform Tools (ADB)
- Usuario en grupo `plugdev` o `adbusers`

```bash
# Ubuntu/Debian
sudo apt update
sudo apt install dotnet-runtime-8.0 adb

# Agregar usuario al grupo USB
sudo usermod -a -G plugdev $USER
```

## Funcionalidades

### Detección de Dispositivos

- Escaneo automático cada 10 segundos
- Reporta estado de conexión a la API
- Maneja múltiples dispositivos simultáneamente

### Ejecución de Comandos

- ADB shell commands
- App installation/uninstallation
- File push/pull
- Screenshots y screen recording
- Logcat streaming

### Monitoreo

- Heartbeat cada minuto a la API
- Logs detallados de todas las operaciones
- Métricas de rendimiento

## Comandos Disponibles

### Gestión del Servicio

**Windows:**

```powershell
.\install-agent.ps1 -Action install    # Instalar servicio
.\install-agent.ps1 -Action start      # Iniciar servicio
.\install-agent.ps1 -Action stop       # Detener servicio
.\install-agent.ps1 -Action restart    # Reiniciar servicio
.\install-agent.ps1 -Action uninstall  # Desinstalar servicio
.\install-agent.ps1 -Action status     # Ver estado
.\install-agent.ps1 -Action test       # Verificar prerequisitos
```

**Linux:**

```bash
sudo ./install-agent.sh install    # Instalar servicio
sudo ./install-agent.sh start      # Iniciar servicio
sudo ./install-agent.sh stop       # Detener servicio
sudo ./install-agent.sh restart    # Reiniciar servicio
sudo ./install-agent.sh uninstall  # Desinstalar servicio
./install-agent.sh status           # Ver estado
./install-agent.sh test             # Verificar prerequisitos
```

### Verificación Manual

```bash
# Verificar que ADB funciona
adb devices

# Probar conectividad con la API
curl https://your-api.company.com/health

# Ver logs del agent (Linux)
journalctl -u mobile-toolkit-agent -f

# Ver logs del agent (Windows)
Get-EventLog -LogName Application -Source "MobileRemoteToolkitAgent" -Newest 10
```

## Arquitectura

```
[API Central] ←→ [Agent Local] ←→ [Dispositivos Android]
     ↓               ↓                    ↓
[Kubernetes]   [Windows/Linux]      [USB Connection]
```

### Flujo de Comunicación

1. **Registro**: Agent se registra con la API al iniciarse
2. **Heartbeat**: Envía señal de vida cada minuto
3. **Device Discovery**: Reporta dispositivos conectados
4. **Command Execution**: Recibe y ejecuta comandos de la API
5. **Response**: Envía resultados de vuelta a la API

## Seguridad

### Autenticación

- API Key requerida para todas las comunicaciones
- Validación de certificados SSL/TLS

### Autorización

- Lista blanca de comandos permitidos
- Validación de parámetros de entrada
- Logs de auditoría completos

### Aislamiento

- Ejecución con usuario de servicio dedicado
- Restricciones de sistema de archivos
- Límites de recursos

## Troubleshooting

### Agent no se conecta a la API

```bash
# Verificar conectividad
curl -I https://your-api.company.com/health

# Verificar configuración
cat /etc/mobile-toolkit-agent/appsettings.json

# Ver logs detallados
journalctl -u mobile-toolkit-agent -f
```

### No detecta dispositivos

```bash
# Verificar ADB
adb devices

# Verificar permisos USB (Linux)
ls -la /dev/bus/usb/
groups $USER

# Reiniciar ADB server
adb kill-server
adb start-server
```

### Comandos fallan

```bash
# Verificar logs del agent
journalctl -u mobile-toolkit-agent -n 50

# Probar comando manualmente
adb shell "comando-que-falla"

# Verificar timeout de comandos
# (ajustar CommandTimeout en appsettings.json)
```

## Métricas y Monitoreo

### Endpoints de Health Check

- `/health` - Estado general del agent
- `/devices` - Lista de dispositivos conectados
- `/metrics` - Métricas de rendimiento

### Logs Importantes

- `Agent started` - Agent iniciado correctamente
- `Device connected/disconnected` - Cambios en dispositivos
- `Command executed` - Comandos ejecutados
- `API communication error` - Errores de comunicación

### Alertas Recomendadas

- Agent desconectado por más de 5 minutos
- Fallos de comando > 10% en última hora
- No hay dispositivos detectados por más de 30 minutos
- Errores de comunicación con API > 5 en 10 minutos
