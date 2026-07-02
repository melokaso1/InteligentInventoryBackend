# El Plonsazo — Backend API

API REST en **.NET 10** con arquitectura **hexagonal** (puertos y adaptadores): dominio desacoplado, casos de uso en Application e infraestructura intercambiable.

## Arquitectura

```
Backend/
├── Api/                 # Adaptador HTTP: controllers, DTOs, mapping
├── Application/         # Casos de uso, puertos (interfaces), modelos
├── Domain/              # Entidades y enums
├── Infrastructure/      # EF Core, repositorios, integraciones
└── ElPlonsazo.slnx
```

| Capa | Responsabilidad |
|------|-----------------|
| **Domain** | Reglas y entidades puras (`Product`, `Sale`, `Invoice`, …) |
| **Application** | Servicios de aplicación (`ProductService`, `SaleService`, `ChatService`, …) |
| **Infrastructure** | `AppDbContext`, repositorios, `ChatbotGateway` → FastAPI |
| **Api** | Endpoints REST, validación de entrada, mapeo a DTOs |

## Requisitos

- .NET SDK **10.0**
- Docker (PostgreSQL en puerto **5433**)

## Configuración

`Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=elplonsazo;Username=elplonsazo;Password=elplonsazo_dev"
  },
  "Chatbot": {
    "BaseUrl": "http://localhost:8000"
  }
}
```

## Ejecución

### Arranque completo (4 servicios)

Para que login, dashboard, chatbot y facturas funcionen en local, levanta **todos** los servicios en este orden:

```bash
# 1. Base de datos (desde la raíz del monorepo)
docker compose up -d

# 2. API .NET — http://localhost:5151
cd Backend/Api
dotnet run

# 3. Chatbot FastAPI — http://localhost:8000
cd LLMChatBot
python run.py

# 4. Frontend Vite — http://localhost:5173
cd Frontend
pnpm dev
```

| Servicio | Puerto | Obligatorio para |
|----------|--------|------------------|
| PostgreSQL (Docker) | 5433 | API, persistencia |
| API .NET | **5151** | Login, CRUD, proxy chat |
| FastAPI chatbot | **8000** | `/chatbot` (consultas IA) |
| Vite dev server | 5173 | UI en navegador |

### Frontend: localhost y ngrok

El proxy de Vite (`Frontend/vite.config.ts`) reenvía `/api` a `http://127.0.0.1:5151`. Tanto **localhost:5173** como un túnel **ngrok** usan el mismo proxy: en ambos casos la API .NET debe estar activa en el puerto **5151**.

En desarrollo, `Frontend/src/api/client.ts` usa `API_BASE = ''` (sin `VITE_API_URL`), de modo que las peticiones pasan por el proxy de Vite. No definas `VITE_API_URL` salvo que quieras apuntar a un host distinto.

Si el login devuelve **502 Bad Gateway**, la API no está corriendo o no escucha en `:5151`.

### Códigos de error HTTP (desarrollo local)

| Código | Endpoint típico | Causa | Solución |
|--------|-----------------|-------|----------|
| **502** | `/api/auth/login`, `/api/dashboard/*` | Vite no alcanza la API .NET en `http://127.0.0.1:5151` | `cd Backend/Api && dotnet run` |
| **503** | `/api/chat/message`, `/api/chat/health` | La API responde pero FastAPI no está en `:8000` | `cd LLMChatBot && python run.py` |
| **401** | Cualquier endpoint autenticado | Token JWT expirado o inválido | Volver a iniciar sesión |

En Windows puedes levantar los 4 servicios con:

```powershell
.\scripts\start-dev.ps1
```

### Antes de compilar (MSB3026 / DLL bloqueadas)

Si `dotnet build` falla con *"El archivo se ha bloqueado por: Api (PID)"*, detén el proceso de la API antes de compilar:

```powershell
# Detener por nombre (recomendado)
taskkill /IM Api.exe /F

# O por PID concreto (sustituye el número del error)
taskkill /PID 74976 /F

cd Backend/Api
dotnet build
```

Flujo recomendado: `Ctrl+C` en la terminal de `dotnet run` → si persiste el bloqueo, `taskkill /IM Api.exe /F` → compilar → volver a `dotnet run`.

### Solo la API

```bash
# Desde la raíz del monorepo
docker compose up -d

cd Backend/Api
dotnet run
```

La API escucha en **http://localhost:5151**.

Al iniciar en Development:

1. Aplica migraciones pendientes (`Database.MigrateAsync`).
2. Siembra datos si no hay productos (catálogo El Plonsazo, ventas y facturas de ejemplo).

## Migraciones EF Core

Las migraciones viven en `Infrastructure/Persistence/Migrations/`.

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

## Autenticación

Al iniciar la API se crea automáticamente un usuario administrador si no existe:

| Campo | Valor |
|-------|-------|
| Email | `admin@elplonsazo.com` |
| Contraseña | `Admin123!` |

Los usuarios que se registran reciben siempre el rol **Cliente**; no pueden cambiar su rol ni escalar privilegios vía API.

| Endpoint | Descripción |
|----------|-------------|
| `POST /api/auth/login` | Inicio de sesión → JWT + datos de usuario con `role` |
| `POST /api/auth/register` | Registro (solo rol Cliente) |
| `GET /api/auth/me` | Usuario autenticado actual |

Los endpoints de gestión (dashboard, productos, inventario, ventas, facturas) requieren rol **Admin**. El chatbot requiere autenticación (Admin o Cliente).

## Endpoints principales

| Recurso | Ruta base | Notas |
|---------|-----------|-------|
| Dashboard | `GET /api/dashboard/kpis`, `/low-stock`, `/activity` | KPIs y actividad reciente |
| Productos | `GET/POST/PUT/PATCH/DELETE /api/products` | CRUD + `GET /stats` |
| Inventario | `GET /api/inventory`, `/stats`, `/movements` | Stock y movimientos |
| Ventas | `GET/POST /api/sales`, `GET /metrics` | |
| Venta chatbot | `POST /api/sales/from-chatbot` | Transaccional (stock + factura) |
| Facturas | `GET/POST /api/invoices`, `/stats`, `/{id}/pdf` | |
| Chat (proxy) | `POST /api/chat/message`, `GET /api/chat/health` | Reenvía al servicio FastAPI |

OpenAPI en Development: `GET /openapi/v1.json`

## Normalización

- **JSON:** `Program.cs` fuerza `JsonNamingPolicy.CamelCase` en serialización.
- **Paginación:** todos los listados devuelven `PagedResponseDto<T>` con `items`, `totalCount`, `page`, `pageSize`.
- **SKU:** `ProductService` normaliza códigos a mayúsculas (`PLZ-XXX-001`).
- **Filtros:** categorías y almacenes comparan con `ToLowerInvariant`; códigos de producto con `ToUpperInvariant`.
- **Enums:** `EntityMappers` traduce dominio → strings snake_case para el frontend (`active`, `out_of_stock`, `chatbot`, …).

## Venta desde chatbot

`POST /api/sales/from-chatbot` ejecuta en una transacción:

1. Valida producto y stock disponible.
2. Crea la venta con origen `chatbot`.
3. Descuenta inventario y registra movimiento.
4. Genera factura asociada.

El chatbot FastAPI invoca este endpoint mediante `dotnet_tools.create_sale`.

## CORS

Orígenes permitidos: `http://localhost:5173`, `http://localhost:5174`.

## Solución

```bash
dotnet build ElPlonsazo.slnx
dotnet test   # si hay proyectos de prueba
```
