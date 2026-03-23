// notification_microservice.cpp — Standalone gRPC server for the extracted
// NotificationService.  This is the microservice that runs independently
// from the monolith.  The monolith's RemoteNotificationService (in
// INotificationService.hpp) acts as the gRPC client that talks to this server.
//
// Build as a separate CMake target / binary.
//
// Compile (example):
//   g++ -std=c++20 -o notification_service notification_microservice.cpp \
//       notification.pb.cc notification.grpc.pb.cc \
//       -lgrpc++ -lprotobuf -lpthread

#include <iostream>
#include <string>
#include <vector>
#include <mutex>
#include <memory>
#include <chrono>

#include <grpcpp/grpcpp.h>
#include "notification.grpc.pb.h"   // generated from proto/notification.proto

namespace shopcore::notification_svc {

// The microservice's own data store (replaces the monolith's
// MonolithDatabase::notification_log for this bounded context).
struct NotificationRecord {
    std::string recipient_id;
    std::string channel;
    std::string subject;
    std::string body;
    std::string timestamp;
};

class NotificationStore {
public:
    void append(NotificationRecord record) {
        std::lock_guard<std::mutex> lock(mu_);
        records_.push_back(std::move(record));
    }

    std::size_t size() const {
        std::lock_guard<std::mutex> lock(mu_);
        return records_.size();
    }

private:
    mutable std::mutex mu_;
    std::vector<NotificationRecord> records_;
};

// ---------------------------------------------------------------------------
// gRPC service implementation
// ---------------------------------------------------------------------------
class NotificationServiceImpl final
    : public notification::NotificationService::Service {
public:
    explicit NotificationServiceImpl(std::shared_ptr<NotificationStore> store)
        : store_(std::move(store)) {}

    grpc::Status SendNotification(
            grpc::ServerContext* context,
            const notification::SendNotificationRequest* request,
            notification::SendNotificationResponse* response) override {

        const auto& channel = request->channel();
        const auto& recipient = request->recipient_id();

        // Simulate sending
        if (channel == "email") {
            std::cout << "[EMAIL] To user " << recipient
                      << ": " << request->subject() << "\n";
        } else if (channel == "sms") {
            std::cout << "[SMS] To user " << recipient
                      << ": " << request->body() << "\n";
        } else {
            std::cout << "[PUSH] To user " << recipient
                      << ": " << request->body() << "\n";
        }

        // Persist to the microservice's own store
        store_->append({
            .recipient_id = recipient,
            .channel      = channel,
            .subject      = request->subject(),
            .body         = request->body(),
            .timestamp    = "2025-06-15T12:00:00Z",  // placeholder
        });

        response->set_success(true);
        return grpc::Status::OK;
    }

private:
    std::shared_ptr<NotificationStore> store_;
};

}  // namespace shopcore::notification_svc


// ---------------------------------------------------------------------------
// Entry point — start the gRPC server
// ---------------------------------------------------------------------------
int main(int argc, char** argv) {
    std::string listen_addr = "0.0.0.0:50051";
    if (argc > 1) {
        listen_addr = argv[1];
    }

    auto store = std::make_shared<shopcore::notification_svc::NotificationStore>();
    shopcore::notification_svc::NotificationServiceImpl service(store);

    grpc::ServerBuilder builder;
    builder.AddListeningPort(listen_addr, grpc::InsecureServerCredentials());
    builder.RegisterService(&service);

    std::unique_ptr<grpc::Server> server(builder.BuildAndStart());
    std::cout << "Notification microservice listening on " << listen_addr << "\n";
    server->Wait();

    return 0;
}
