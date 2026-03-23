# The Strangler Fig Pattern for Monolith-to-Microservices Migration

## Overview

The Strangler Fig pattern is an incremental migration strategy named after the strangler fig tree, which grows around an existing tree and gradually replaces it. In software, it means you progressively replace specific pieces of a monolith with new microservices, while keeping the system running the entire time.

## Core Principles

1. **Never rewrite from scratch.** The monolith continues to run in production during the entire migration.
2. **Extract one bounded context at a time.** Start with the module that has the fewest incoming dependencies.
3. **Use a facade (proxy/router)** that sits in front of the monolith. Initially all traffic goes to the monolith. As each service is extracted, the facade routes the relevant traffic to the new service.
4. **Coexistence is the default state.** The monolith and microservices run side-by-side for extended periods.
5. **Each extraction is independently deployable and testable.**

## Step-by-Step Process

### Step 1 — Map Dependencies

Analyze the monolith and draw a dependency graph of all modules. Identify:
- Which modules call which other modules
- Shared data structures and database tables
- Cross-cutting concerns (logging, auth, notifications)

### Step 2 — Identify Bounded Contexts

Group related functionality into bounded contexts. For an e-commerce monolith, typical bounded contexts are:
- **Authentication & Users** — registration, login, user profiles
- **Product Catalog** — product CRUD, search, categories
- **Inventory** — stock management, reservations, restocking
- **Orders** — order placement, cancellation, history
- **Payments** — payment processing, refunds
- **Notifications** — email, SMS, push notifications

### Step 3 — Choose the First Service to Extract

Pick the module with:
- Fewest inbound dependencies (other modules that call it)
- Clearest API boundary
- Lowest risk if something goes wrong

**Notifications** is often the best first candidate because:
- It has zero inbound data dependencies (it only receives messages)
- Other modules call it, but it doesn't call other modules
- Failures in notifications don't break core business flows

### Step 4 — Build the New Microservice

Create the new service as an independent, deployable unit with:
- Its own data store (if applicable)
- A well-defined API (REST, gRPC, or message queue)
- Health checks and monitoring
- Its own build and deployment pipeline

### Step 5 — Create the Facade / Anti-Corruption Layer

The facade sits between the monolith and the new service:
- Intercepts calls that the monolith used to make internally
- Translates between the monolith's internal data structures and the new service's API
- Handles fallback: if the new service is down, optionally fall back to the monolith's old code

### Step 6 — Migrate Traffic Gradually

- Start with a small percentage of traffic going to the new service (canary)
- Monitor errors, latency, and correctness
- Gradually increase to 100%
- Remove the old code from the monolith once fully migrated

### Step 7 — Repeat for the Next Service

Continue extracting services one by one, always choosing the next module with the fewest remaining dependencies.

## Recommended Extraction Order (E-Commerce Example)

1. **Notifications** — pure leaf service, no dependencies on others
2. **Authentication** — used by many, but has a clear API surface (login, register, validate token)
3. **Product Catalog** — read-heavy, can be independently scaled
4. **Inventory** — coupled to catalog but can use events to stay in sync
5. **Payments** — sensitive, benefits from isolation and dedicated security
6. **Orders** — most coupled, extract last after all its dependencies are already services

## Anti-Patterns to Avoid

- **Shared database**: Each microservice must own its data. Sharing a database between services defeats the purpose.
- **Synchronous chains**: Service A calls B calls C calls D — this creates a distributed monolith. Use async messaging where possible.
- **Big-bang migration**: Extracting everything at once is a rewrite in disguise, not Strangler Fig.
- **Ignoring the facade**: Without a routing layer, you can't do gradual migration.

## Facade Implementation Patterns

### HTTP Reverse Proxy
Route HTTP requests based on URL path. `/api/notifications/*` goes to the notification service; everything else goes to the monolith.

### Message Queue (Event-Driven)
The monolith publishes events (e.g., "OrderPlaced") to a message broker. The new notification service subscribes to these events instead of being called directly.

### API Gateway
A dedicated gateway (e.g., Kong, Envoy, or custom) that manages routing, authentication, rate limiting, and load balancing across the monolith and microservices.

## C++ Specific Considerations

- Use gRPC for inter-service communication (protobuf gives strong typing)
- Each service should be a separate CMake project / build target
- Use `std::async` or a thread pool for non-blocking service calls from the monolith during transition
- Define service interfaces as pure abstract classes (interface segregation) before extracting
- Use dependency injection to swap between local (in-monolith) and remote (microservice) implementations

## Key Data Structures for the Facade

The anti-corruption layer should translate between monolith internal types and service API types:

```cpp
// Monolith uses internal struct
struct Notification {
    int user_id;
    std::string type;
    std::string subject;
    std::string body;
};

// New service uses a DTO / protobuf message
// NotificationRequest {
//   string recipient_id = 1;
//   string channel = 2;       // "email", "sms", "push"
//   string title = 3;
//   string content = 4;
// }

// The facade translates between them:
// NotificationFacade::send(Notification n) {
//   auto req = to_grpc_request(n);
//   notification_stub_->Send(req);
// }
```
