# Microservice Templates & Conventions

## Standard Microservice Structure (C++)

Each extracted microservice should follow this directory layout:

```
service-name/
├── CMakeLists.txt
├── Dockerfile
├── proto/
│   └── service_name.proto        # gRPC service definition
├── include/
│   └── service_name/
│       ├── service.hpp           # Service interface
│       ├── repository.hpp        # Data access
│       └── models.hpp            # Domain models (DTOs)
├── src/
│   ├── main.cpp                  # Entry point, server startup
│   ├── service_impl.cpp          # Business logic
│   └── repository_impl.cpp       # Data access implementation
├── tests/
│   ├── unit/
│   └── integration/
└── config/
    └── service.yaml              # Runtime configuration
```

## gRPC Service Definition Template

```protobuf
syntax = "proto3";
package shopcore.<service_name>;

service <ServiceName>Service {
    rpc <MethodName> (<RequestType>) returns (<ResponseType>);
    rpc HealthCheck (Empty) returns (HealthResponse);
}

message Empty {}

message HealthResponse {
    bool healthy = 1;
    string version = 2;
}
```

## Service Interface Pattern (C++)

Each service should define a pure abstract interface so the monolith can swap between local and remote implementations:

```cpp
// INotificationService.hpp — the interface both monolith and microservice implement
class INotificationService {
public:
    virtual ~INotificationService() = default;
    virtual void send_email(int user_id, const std::string& subject,
                            const std::string& body) = 0;
    virtual void send_sms(int user_id, const std::string& message) = 0;
};

// LocalNotificationService — the old monolith implementation (wraps existing code)
class LocalNotificationService : public INotificationService { ... };

// RemoteNotificationService — calls the new microservice via gRPC
class RemoteNotificationService : public INotificationService {
    std::unique_ptr<NotificationService::Stub> stub_;
public:
    void send_email(int user_id, const std::string& subject,
                    const std::string& body) override {
        // Translate to gRPC request and send
    }
};
```

## Facade / Router Pattern

The facade decides which implementation to use, enabling gradual migration:

```cpp
class NotificationFacade : public INotificationService {
    std::shared_ptr<INotificationService> local_;
    std::shared_ptr<INotificationService> remote_;
    bool use_remote_ = false;   // feature flag

public:
    NotificationFacade(std::shared_ptr<INotificationService> local,
                       std::shared_ptr<INotificationService> remote)
        : local_(local), remote_(remote) {}

    void enable_remote() { use_remote_ = true; }

    void send_email(int user_id, const std::string& subject,
                    const std::string& body) override {
        if (use_remote_) {
            try {
                remote_->send_email(user_id, subject, body);
            } catch (...) {
                // Fallback to local if remote fails
                local_->send_email(user_id, subject, body);
            }
        } else {
            local_->send_email(user_id, subject, body);
        }
    }

    void send_sms(int user_id, const std::string& message) override {
        if (use_remote_) {
            try {
                remote_->send_sms(user_id, message);
            } catch (...) {
                local_->send_sms(user_id, message);
            }
        } else {
            local_->send_sms(user_id, message);
        }
    }
};
```

## Dependency Injection Setup

In the monolith's main() or initialization code, swap the implementation via configuration:

```cpp
// During migration — use facade with feature flag
auto local_notif = std::make_shared<LocalNotificationService>();
auto remote_notif = std::make_shared<RemoteNotificationService>(grpc_channel);
auto notification_service = std::make_shared<NotificationFacade>(local_notif, remote_notif);

// Enable remote when ready
if (config.get("notifications.use_remote", false)) {
    notification_service->enable_remote();
}

// Pass to all services that need notifications
OrderService order_service(notification_service);
AuthService auth_service(notification_service);
InventoryService inventory_service(notification_service);
```

## Event-Driven Alternative

Instead of synchronous gRPC calls, use an event bus / message queue:

```cpp
class EventBus {
    std::map<std::string, std::vector<std::function<void(const std::string&)>>> subscribers_;
public:
    void publish(const std::string& event_type, const std::string& payload) {
        if (subscribers_.count(event_type)) {
            for (auto& handler : subscribers_[event_type]) {
                handler(payload);
            }
        }
    }

    void subscribe(const std::string& event_type,
                   std::function<void(const std::string&)> handler) {
        subscribers_[event_type].push_back(handler);
    }
};

// Monolith publishes events instead of calling NotificationService directly:
// event_bus.publish("order.placed", order_json);
// event_bus.publish("user.registered", user_json);

// Notification microservice subscribes:
// event_bus.subscribe("order.placed", [](const std::string& payload) {
//     send_order_confirmation(parse_order(payload));
// });
```

## Dockerfile Template

```dockerfile
FROM ubuntu:22.04 AS builder
RUN apt-get update && apt-get install -y cmake g++ libgrpc++-dev protobuf-compiler-grpc
WORKDIR /app
COPY . .
RUN cmake -B build -DCMAKE_BUILD_TYPE=Release && cmake --build build

FROM ubuntu:22.04
RUN apt-get update && apt-get install -y libgrpc++1
COPY --from=builder /app/build/service /usr/local/bin/service
EXPOSE 50051
CMD ["service"]
```

## Health Check Endpoint

Every microservice must expose a health check:

```cpp
grpc::Status HealthCheck(grpc::ServerContext* context,
                         const Empty* request,
                         HealthResponse* response) override {
    response->set_healthy(true);
    response->set_version("1.0.0");
    return grpc::Status::OK;
}
```
