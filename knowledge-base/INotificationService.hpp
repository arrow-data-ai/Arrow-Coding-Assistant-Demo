#pragma once
// INotificationService.hpp — Pure abstract interface for the Notification bounded context.
// Both the monolith's local implementation and the remote gRPC client implement this.
// The facade also inherits from this so it can be injected anywhere a notification
// service is expected.
//
// IMPORTANT: This interface uses plain C++ types only. gRPC / protobuf details
// belong in RemoteNotificationService, NOT here.

#include <string>
#include <memory>

namespace shopcore {

class INotificationService {
public:
    virtual ~INotificationService() = default;

    virtual void send_email(int user_id,
                            const std::string& subject,
                            const std::string& body) = 0;

    virtual void send_sms(int user_id,
                          const std::string& message) = 0;
};

// ---------------------------------------------------------------------------
// LocalNotificationService — wraps the original monolith static methods
// so they satisfy the interface. Used as the "local" leg of the facade.
// ---------------------------------------------------------------------------
class LocalNotificationService : public INotificationService {
public:
    void send_email(int user_id,
                    const std::string& subject,
                    const std::string& body) override {
        // Delegates to the original static NotificationService code
        // (which writes to MonolithDatabase::instance().notification_log)
        NotificationService::send_email(user_id, subject, body);
    }

    void send_sms(int user_id, const std::string& message) override {
        NotificationService::send_sms(user_id, message);
    }
};

// ---------------------------------------------------------------------------
// RemoteNotificationService — calls the extracted Notification microservice
// via gRPC. gRPC types are confined to this class only.
// ---------------------------------------------------------------------------
class RemoteNotificationService : public INotificationService {
public:
    explicit RemoteNotificationService(std::shared_ptr<grpc::Channel> channel)
        : stub_(notification::NotificationService::NewStub(channel)) {}

    void send_email(int user_id,
                    const std::string& subject,
                    const std::string& body) override {
        notification::SendNotificationRequest req;
        req.set_recipient_id(std::to_string(user_id));
        req.set_channel("email");
        req.set_subject(subject);
        req.set_body(body);

        notification::SendNotificationResponse resp;
        grpc::ClientContext ctx;
        grpc::Status status = stub_->SendNotification(&ctx, req, &resp);
        if (!status.ok()) {
            std::cerr << "[RemoteNotification] gRPC error: "
                      << status.error_message() << "\n";
        }
    }

    void send_sms(int user_id, const std::string& message) override {
        notification::SendNotificationRequest req;
        req.set_recipient_id(std::to_string(user_id));
        req.set_channel("sms");
        req.set_subject("SMS");
        req.set_body(message);

        notification::SendNotificationResponse resp;
        grpc::ClientContext ctx;
        stub_->SendNotification(&ctx, req, &resp);
    }

private:
    std::unique_ptr<notification::NotificationService::Stub> stub_;
};

} // namespace shopcore
