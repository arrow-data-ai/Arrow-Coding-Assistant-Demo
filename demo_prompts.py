"""
Demo prompts for Strangler Fig Migration Assistant
Monolith-to-Microservices code generation demo

This file contains demo prompts that showcase the assistant's ability to
analyze a C++ monolith and generate microservice extraction code using
the Strangler Fig pattern.
"""

DEMO_PROMPTS = {
    "1_analyze": {
        "title": "Prompt #1 — Analyze the Monolith",
        "prompt": "Analyze the ShopCore e-commerce monolith. Identify all bounded contexts, map the dependencies between modules, and recommend which service to extract first using the Strangler Fig pattern. Explain your reasoning.",
        "description": "Analyze monolith structure and identify bounded contexts"
    },
    "2_rag_contrast": {
        "title": "Prompt #2 — RAG On vs Off Contrast",
        "prompt": "What is the dependency graph of the ShopCore monolith? Which modules call NotificationService and why is it the best candidate for first extraction?",
        "description": "Turn off RAG to show generic vs knowledge-grounded answer"
    },
    "3_extract_notifications": {
        "title": "Prompt #3 — Extract Notification Service",
        "prompt": """Extract the NotificationService from the ShopCore monolith as an independent microservice using the Strangler Fig pattern.

Generate the following:
1. An INotificationService pure abstract interface
2. The new standalone NotificationService microservice (with gRPC API)
3. A RemoteNotificationService client that calls the microservice
4. A NotificationFacade that routes between local and remote implementations
5. The modified monolith code showing how OrderService, AuthService, and InventoryService now use the facade instead of calling NotificationService directly

Use modern C++20. Include all headers.""",
        "description": "Generate complete microservice extraction with facade"
    },
    "4_extract_auth": {
        "title": "Prompt #4 — Extract Auth Service",
        "prompt": """The NotificationService has been successfully extracted. Now extract AuthService as the second microservice.

Generate:
1. An IAuthService interface with register_user, login, and deactivate_user methods
2. The new Auth microservice with JWT token generation
3. A RemoteAuthService client
4. An AuthFacade for gradual migration
5. Show how the monolith's main code changes to use the facade

Remember that AuthService currently depends on NotificationService (for welcome emails), which is now a separate microservice. Handle this cross-service dependency.""",
        "description": "Extract second service with cross-service dependency"
    },
    "5_event_driven": {
        "title": "Prompt #5 — Event-Driven Decoupling",
        "prompt": """The ShopCore monolith has tight coupling: OrderService directly calls NotificationService to send order confirmations, payment failure alerts, and cancellation emails.

Refactor this to use an event-driven architecture:
1. Define an EventBus class
2. Define domain events: OrderPlaced, OrderCancelled, PaymentFailed
3. Show the modified OrderService that publishes events instead of calling NotificationService
4. Show the NotificationService subscriber that listens for these events
5. Include the event serialization/deserialization

Generate complete C++20 code.""",
        "description": "Replace tight coupling with event-driven architecture"
    },
    "6_grpc_definition": {
        "title": "Prompt #6 — Generate gRPC Service Definitions",
        "prompt": """Generate the complete gRPC protobuf definitions (.proto files) for extracting the following services from the ShopCore monolith:

1. NotificationService — send_email, send_sms
2. AuthService — register_user, login, validate_token, deactivate_user
3. CatalogService — add_product, get_product, search_products, get_by_category
4. InventoryService — check_stock, reserve_stock, restock

For each, define the request/response messages based on the monolith's existing data structures. Include health check endpoints.""",
        "description": "Generate protobuf service definitions"
    },
    "7_facade_with_fallback": {
        "title": "Prompt #7 — Facade with Fallback & Monitoring",
        "prompt": """Generate a production-grade facade for the PaymentService extraction that includes:

1. Feature-flag-based routing (local vs remote)
2. Automatic fallback to local implementation if the remote service is unavailable
3. Circuit breaker pattern (stop calling remote after N consecutive failures, retry after timeout)
4. Metrics collection (latency, success/failure counts)
5. Logging for debugging

Generate complete C++20 code with all headers.""",
        "description": "Production-grade facade with circuit breaker"
    },
    "8_migration_plan": {
        "title": "Prompt #8 — Full Migration Roadmap",
        "prompt": """Create a complete 6-phase migration plan to decompose the ShopCore monolith into microservices using the Strangler Fig pattern.

For each phase:
- Which service to extract and why (dependency analysis)
- What the facade/anti-corruption layer looks like
- What changes to the monolith code
- What risks to watch for
- Definition of done (when to remove old code)

Include a timeline estimate and a dependency diagram showing the state of the system after each phase.""",
        "description": "Complete migration roadmap with phases"
    },
}
