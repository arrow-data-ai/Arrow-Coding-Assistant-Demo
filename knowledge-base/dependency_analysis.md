# ShopCore Monolith — Dependency Analysis

## Module Dependency Graph

This document maps the coupling between modules in the ShopCore monolith to guide extraction order.

## Direct Dependencies (who calls whom)

```
AuthService
├── calls → NotificationService::send_email()
└── calls → MonolithDatabase (users table)

CatalogService
└── calls → MonolithDatabase (products table)

InventoryService
├── calls → MonolithDatabase (products table)
└── calls → NotificationService::send_email()  [low stock alerts]

PaymentService
└── calls → (no other services — self-contained logic)

OrderService
├── calls → CatalogService::get_product()
├── calls → InventoryService::check_stock()
├── calls → InventoryService::reserve_stock()
├── calls → InventoryService::restock()      [on cancellation]
├── calls → PaymentService::process_payment()
├── calls → PaymentService::refund()          [on cancellation]
├── calls → NotificationService::send_email()
├── calls → NotificationService::send_sms()
└── calls → MonolithDatabase (orders table, users table)

NotificationService
└── calls → MonolithDatabase (notification_log)
```

## Inbound Dependency Count (who depends on this module)

| Module              | Called by                                      | Inbound count |
|---------------------|------------------------------------------------|---------------|
| NotificationService | AuthService, InventoryService, OrderService    | 3             |
| MonolithDatabase    | All modules                                    | 5             |
| CatalogService      | OrderService                                   | 1             |
| InventoryService    | OrderService                                   | 1             |
| PaymentService      | OrderService                                   | 1             |
| AuthService         | (none — only called from main/external)        | 0             |
| OrderService        | (none — only called from main/external)        | 0             |

## Recommended Strangler Fig Extraction Order

### Phase 1: NotificationService (Leaf node — easiest)
- **Why first:** NotificationService is called BY other modules but doesn't call any other business modules. It only writes to its own log.
- **Migration approach:** Replace direct `NotificationService::send_email()` calls with an interface + facade. The new notification microservice receives events via a message queue or gRPC.
- **Risk:** Very low. If the notification service fails, no business transaction is blocked.
- **Monolith change:** Replace static method calls with dependency-injected `INotificationService` interface.

### Phase 2: AuthService (Clear boundary)
- **Why second:** Auth has a clear API (register, login, deactivate). It only depends on NotificationService (already extracted) and the database.
- **Migration approach:** New auth microservice owns the users table. Issues JWT tokens. Monolith validates tokens instead of checking the local user map.
- **Risk:** Medium. Auth failures block all authenticated operations.
- **Monolith change:** Replace `AuthService::login()` with HTTP/gRPC call to auth service. Add JWT validation middleware.

### Phase 3: CatalogService (Read-heavy, easy to scale)
- **Why third:** Product catalog is mostly read operations. Can be independently cached and scaled.
- **Migration approach:** New catalog service owns the products table. Exposes REST/gRPC for product queries.
- **Monolith change:** OrderService calls the catalog service API instead of `CatalogService::get_product()` directly.

### Phase 4: InventoryService (Coupled to Catalog)
- **Why fourth:** Inventory manages product stock. After catalog is extracted, inventory can own stock data separately.
- **Migration approach:** Inventory service manages stock quantities. Listens to catalog events for product updates. Exposes check_stock / reserve_stock / restock API.
- **Monolith change:** OrderService calls inventory service API.

### Phase 5: PaymentService (Sensitive — benefits from isolation)
- **Why fifth:** Payment is self-contained but sensitive. Extracting it allows dedicated security, PCI compliance, and audit logging.
- **Migration approach:** New payment service with its own encrypted data store. Exposes process_payment / refund API.
- **Monolith change:** OrderService calls payment service API.

### Phase 6: OrderService (Last — after all dependencies are services)
- **Why last:** OrderService is the most coupled module. It calls Catalog, Inventory, Payment, and Notification. Extract it only after all dependencies are already microservices.
- **Migration approach:** Order service orchestrates the other services. Owns the orders table.
- **Monolith removal:** At this point, the monolith is fully replaced.

## Shared Data Structures

The following structs are used across module boundaries and need API contracts:
- `User` — used by AuthService, OrderService
- `Product` — used by CatalogService, InventoryService, OrderService
- `Order` / `OrderItem` — used by OrderService, PaymentService
- `Notification` — used by NotificationService (internal only after extraction)
- `PaymentResult` — used by PaymentService, OrderService

Each extracted service should define its own DTOs / protobuf messages rather than sharing the monolith structs directly.
