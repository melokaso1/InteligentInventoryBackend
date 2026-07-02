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

```bash
# Desde la raíz del monorepo
docker compose up -d

cd Api
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

## Endpoints principales

| Recurso | Ruta base | Notas |
|---------|-----------|-------|
| Dashboard | `GET /api/dashboard/kpis`, `/low-stock`, `/activity` | KPIs y actividad reciente |
| Productos | `GET/POST/PUT/PATCH/DELETE /api/products` | CRUD + `GET /stats` |
| Inventario | `GET /api/inventory`, `/stats`, `/movements` | Stock y movimientos |
| Ventas | `GET/POST /api/sales`, `GET /metrics` | |
| Venta chatbot | `POST /api/sales/from-chatbot` | Transaccional (stock + factura) |
| Facturas | `GET /api/invoices`, `/stats`, `/{id}/pdf` | |
| Chat (proxy) | `POST /api/chat/message` | Reenvía al servicio FastAPI |

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
