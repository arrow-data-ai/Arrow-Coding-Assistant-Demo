#pragma once
// notification_facade.hpp — Facade / Anti-Corruption Layer for NotificationService.
// Implements INotificationService so it can be injected into any module that
// previously called the static NotificationService directly.
//
// The facade routes between the local (monolith) and remote (microservice)
// implementations based on a feature flag, with automatic fallback.

#include "INotificationService.hpp"
#include <memory>
#include <iostream>

namespace shopcore {

class NotificationFacade : public INotificationService {
public:
    NotificationFacade(std::shared_ptr<INotificationService> local,
                       std::shared_ptr<INotificationService> remote)
        : local_(std::move(local)),
          remote_(std::move(remote)) {}

    void enable_remote()  { use_remote_ = true; }
    void disable_remote() { use_remote_ = false; }
    bool using_remote() const { return use_remote_; }

    void send_email(int user_id,
                    const std::string& subject,
                    const std::string& body) override {
        if (use_remote_) {
            try {
                remote_->send_email(user_id, subject, body);
            } catch (...) {
                std::cerr << "[Facade] Remote send_email failed, falling back to local\n";
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
                std::cerr << "[Facade] Remote send_sms failed, falling back to local\n";
                local_->send_sms(user_id, message);
            }
        } else {
            local_->send_sms(user_id, message);
        }
    }

private:
    std::shared_ptr<INotificationService> local_;
    std::shared_ptr<INotificationService> remote_;
    bool use_remote_ = false;
};

// ---------------------------------------------------------------------------
// Dependency Injection wiring — called from the monolith's main() to build
// the facade and inject it into all services that need notifications.
// ---------------------------------------------------------------------------
//
// Usage in main():
//
//   #include "notification_facade.hpp"
//
//   int main() {
//       // 1. Build local + remote implementations
//       auto local_notif  = std::make_shared<LocalNotificationService>();
//       auto remote_notif = std::make_shared<RemoteNotificationService>(
//           grpc::CreateChannel("localhost:50051",
//                               grpc::InsecureChannelCredentials()));
//
//       // 2. Wrap in facade
//       auto notifier = std::make_shared<NotificationFacade>(local_notif,
//                                                            remote_notif);
//
//       // 3. Feature-flag: flip to remote when ready
//       if (config.get("notifications.use_remote", false)) {
//           notifier->enable_remote();
//       }
//
//       // 4. Inject into every service that previously called
//       //    NotificationService::send_email() or send_sms() directly
//       OrderService     order_service(notifier);
//       AuthService      auth_service(notifier);
//       InventoryService inventory_service(notifier);
//
//       // 5. Run the application as before
//       // ...
//   }

} // namespace shopcore
