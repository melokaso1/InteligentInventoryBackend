# Instalación — Backend API (El Plonsazo)

Guía paso a paso para levantar la API .NET en local. Para arquitectura, endpoints y modelo de datos, consulta [README.md](./README.md).

## Requisitos previos

| Herramienta | Versión mínima | Verificar |
|-------------|----------------|-----------|
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0** | `dotnet --version` |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Solo para PostgreSQL | `docker --version` |
| Python (solo si el chatbot no arranca solo) | **3.11+** | `python --version` |

> **Docker** en este proyecto sirve únicamente para levantar PostgreSQL. La API, el chatbot y el frontend se ejecutan directamente en el sistema operativo (sin contenedores).

Opcional para la interfaz web:

| Herramienta | Versión | Verificar |
|-------------|---------|-----------|
| [Node.js](https://nodejs.org/) | 20+ | `node --version` |
| pnpm | 9+ | `pnpm --version` |

## Estructura del monorepo

```
Proyecto LLM/
├── Backend/          ← API .NET (esta guía)
│   ├── Api/          ← Punto de entrada: dotnet run
│   ├── docker-compose.yml   ← solo PostgreSQL
│   └── .env
├── LLMChatBot/       ← Chatbot FastAPI (puerto 8000)
└── Frontend/         ← React + Vite (puerto 5173)
```

## Instalación rápida

### 1. Clonar el repositorio

```bash
git clone <url-del-repositorio>
cd "Proyecto LLM/Backend"
```

### 2. Levantar PostgreSQL (Docker)

Docker en este proyecto **solo** levanta la base de datos. No hay contenedores para la API, el chatbot ni el frontend.

```bash
docker compose up -d
```

Esto crea el contenedor `elplonsazo-db` con:

| Parámetro | Valor |
|-----------|-------|
| Host | `localhost` |
| Puerto | **5433** (mapeado al 5432 interno) |
| Base de datos | `elplonsazo` |
| Usuario | `elplonsazo` |
| Contraseña | `elplonsazo_dev` |

Comprobar que está sano:

```bash
docker compose ps
```

Debe mostrar `healthy` en la columna de estado.

### 3. Configurar variables de entorno

El repositorio incluye `Backend/.env` con valores de desarrollo. Si no existe, créalo a partir del ejemplo:

```bash
cp .env.example .env
```

Contenido mínimo (ya viene así por defecto):

```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5151

ConnectionStrings__Default=Host=localhost;Port=5433;Database=elplonsazo;Username=elplonsazo;Password=elplonsazo_dev

Chatbot__AutoStart=true
Chatbot__BaseUrl=http://localhost:8000
Chatbot__ApiKey=elplonsazo-chatbot-dev-key

Jwt__Secret=ElPlonsazo-Dev-Secret-Key-Min-32-Chars!!
Jwt__Issuer=ElPlonsazo
Jwt__Audience=ElPlonsazoApp
Jwt__ExpirationMinutes=480
```

> La API carga `Backend/.env` automáticamente al ejecutar `dotnet run` desde `Api/`.

### 4. Restaurar dependencias y ejecutar la API

```bash
cd Api
dotnet restore
dotnet run
```

La API escucha en **http://localhost:5151**.

Al primer arranque en Development:

1. Aplica migraciones de Entity Framework automáticamente.
2. Siembra el catálogo ficticio (~29 productos `PLZ-*`) si la base está vacía.
3. Crea o sincroniza el usuario administrador.
4. Si `Chatbot__AutoStart=true` y Python está en el PATH, intenta levantar el chatbot FastAPI en el puerto **8000**.

### 5. Verificar que funciona

```bash
# Salud de la API (requiere login o endpoint público según versión)
curl http://localhost:5151/openapi/v1.json

# Login de administrador
curl -X POST http://localhost:5151/api/auth/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"admin@elplonsazo.com\",\"password\":\"Admin123!\"}"
```

Respuesta esperada del login: JSON con `token` y datos del usuario.

## Chatbot (FastAPI)

La API actúa como proxy hacia el chatbot en `http://localhost:8000`. Tienes dos opciones:

### Opción A — Arranque automático (recomendado en desarrollo)

Con `Chatbot__AutoStart=true` en `.env`, la API lanza `uvicorn` si:

- Python 3.11+ está en el PATH.
- El puerto 8000 está libre.
- Existe la carpeta `../LLMChatBot` respecto a `Backend/`.

Requisitos del chatbot:

```bash
cd ../LLMChatBot
python -m venv .venv

# Windows
.venv\Scripts\activate
# Linux / macOS
source .venv/bin/activate

pip install -r requirements.txt
```

El chatbot también necesita su `.env` (incluido en el repo o copiar desde `.env.example`):

```env
DOTNET_API_URL=http://localhost:5151
CHATBOT_API_KEY=elplonsazo-chatbot-dev-key
```

### Opción B — Arranque manual

```bash
cd ../LLMChatBot
python run.py
```

Si el puerto 8000 ya está en uso, `ChatbotHostedService` detecta el proceso existente y no intenta iniciar otro.

Comprobar el chatbot:

```bash
curl http://localhost:8000/health
```

## Frontend (opcional)

Para usar la interfaz completa en el navegador:

```bash
cd ../Frontend
pnpm install
pnpm dev
```

Abre **http://localhost:5173**. El proxy de Vite reenvía `/api` a `http://127.0.0.1:5151`; no hace falta definir `VITE_API_URL` en local.

## Usuario administrador

| Campo | Valor |
|-------|-------|
| Email | `admin@elplonsazo.com` |
| Contraseña | `Admin123!` |
| Nombre | Jhon Alejandro Escobar Lozada |

La contraseña y el nombre se sincronizan en cada arranque de la API si el usuario ya existe.

Los clientes se registran con `POST /api/auth/register` y reciben rol **Cliente**.

## Puertos y servicios

| Servicio | Cómo se ejecuta | Puerto | Obligatorio para |
|----------|-----------------|--------|------------------|
| PostgreSQL | **Docker** | **5433** | Persistencia |
| API .NET | `dotnet run` | **5151** | Login, CRUD, proxy chat |
| FastAPI chatbot | `python run.py` | **8000** | Página `/chatbot` |
| Vite (frontend) | `pnpm dev` | **5173** | UI en navegador |

## Migraciones de base de datos

Las migraciones viven en `Infrastructure/Persistence/Migrations/` y se aplican solas al iniciar la API.

Para crear una nueva migración (desde `Backend/`):

```bash
dotnet ef migrations add NombreMigracion \
  --project Infrastructure \
  --startup-project Api \
  --output-dir Persistence/Migrations
```

Para aplicar manualmente:

```bash
dotnet ef database update --project Infrastructure --startup-project Api
```

## Reiniciar la base de datos

Si el catálogo quedó corrupto o quieres empezar de cero:

```bash
cd Backend
docker compose down -v
docker compose up -d
cd Api
dotnet run
```

En los logs deberías ver algo como `Seed completado: 29 producto(s)…`.

## Solución de problemas

### La API no arranca: error de conexión a PostgreSQL

```
Falló la migración o el seed de la base de datos...
```

1. Comprueba que Docker está en ejecución.
2. Ejecuta `docker compose up -d` desde `Backend/`.
3. Verifica que el puerto **5433** no esté ocupado por otro servicio.

### `dotnet build` falla: DLL bloqueada (Windows)

```
El archivo se ha bloqueado por: Api (PID ...)
```

Detén la API antes de compilar:

```powershell
taskkill /IM Api.exe /F
```

O pulsa `Ctrl+C` en la terminal donde corre `dotnet run`.

### Error 502 en el frontend (login, dashboard)

Vite no alcanza la API en `http://127.0.0.1:5151`. Asegúrate de que `dotnet run` está activo en `Backend/Api`.

### Error 503 en el chatbot

La API responde pero FastAPI no está en el puerto 8000:

1. Activa `Chatbot__AutoStart=true` y reinicia la API, o
2. Ejecuta manualmente `cd LLMChatBot && python run.py`.

### El chatbot no arranca automáticamente

Revisa los logs de la API. Causas habituales:

- Python no está en el PATH.
- Falta `pip install -r requirements.txt` en `LLMChatBot/`.
- El puerto 8000 está ocupado por otro proceso.

### Catálogo incompleto o producto de prueba `TEST-001`

Reinicia la API: en cada arranque se eliminan SKUs de prueba legacy. Si persiste el problema, haz reset de la base (ver sección anterior).

### Login en Netlify no funciona

El frontend en **https://elplonsazo.netlify.app** necesita una API .NET accesible desde internet. Configura `VITE_API_URL` en Netlify apuntando a tu API pública y añade ese origen en CORS (`Program.cs`).

## Compilar la solución

```bash
cd Backend
dotnet build ElPlonsazo.slnx
```

## Siguiente paso

- Documentación completa de la API: [README.md](./README.md)
- Chatbot: [../LLMChatBot/README.md](../LLMChatBot/README.md)
- Frontend: [../Frontend/README.md](../Frontend/README.md)
