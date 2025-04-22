# Eon-Dental-Event-Driven-Project

# 🧾 Order & Payment Microservices (Event-Driven)

This solution demonstrates two ASP.NET Core microservices—**OrderService** and **PaymentService**—that communicate asynchronously using RabbitMQ via an `IEventBus` abstraction.

---

## 📦 Microservices Overview

### 🛒 OrderService
Handles order creation and updates.

- **Creates orders**
- **Publishes** `OrderCreatedEvent` after order creation
- **Subscribes** to `PaymentCompletedEvent` to update order status

### 💳 PaymentService
Manages payments related to orders.

- **Subscribes** to `OrderCreatedEvent` to generate a pending payment
- **Processes** payments via an API
- **Publishes** `PaymentCompletedEvent` once payment is processed

---

## 🔗 Event Flow

1. **OrderService** creates an order ➡ publishes `OrderCreatedEvent`
2. **PaymentService** receives event ➡ creates a pending payment
3. Client triggers payment processing via API ➡ `PaymentCompletedEvent` is published
4. **OrderService** updates order status on receiving `PaymentCompletedEvent`

## 🛠️ Technologies Used

- **ASP.NET Core 8**
- **Entity Framework Core**
- **RabbitMQ**
- **Swagger (OpenAPI)**
- **SQL Server**

---

## ⚙️ Configuration

Please make sure to run "docker-compose up" on the docker-compose.yml file located in the solution's location.
