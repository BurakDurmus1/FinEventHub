# FinEventHub

## Overview

FinEventHub is a .NET 8 microservice-based event processing system developed as a technical assessment.

The system receives high-volume financial transaction events, publishes them to RabbitMQ, processes them asynchronously, and generates daily customer summaries.

The solution is designed around an event-driven architecture and provides reliable processing through idempotency, transactional consistency and retry mechanisms.

---

# Architecture

```
                POST /api/v1/events/batch
                           |
                           ▼
                 Ingestion API
                           |
                     RabbitMQ Exchange
                           |
                    events Queue
                           |
                           ▼
                 Aggregation API
                           |
                           ▼
                     PostgreSQL
                           |
                           ▼
 GET /api/v1/customers/{customerId}/daily-summary
```

---

# Technologies

- .NET 8
- ASP.NET Core Web API
- RabbitMQ
- PostgreSQL
- Entity Framework Core
- FluentValidation
- Docker
- xUnit
- SQLite In-Memory
- Bogus

---

# Solution Structure

```
FinEventHub

├── FinEventHub.Contracts
├── FinEventHub.Ingestion.Api
├── FinEventHub.Aggregation.Api
├── FinEventHub.LoadGenerator
└── FinEventHub.Tests
```

---

# Running the Project

## Infrastructure

```bash
docker compose up --build
```

RabbitMQ Management UI

```
http://localhost:15672
```

Default credentials

```
guest
guest
```

---

## Start Services

Run

- FinEventHub.Ingestion.Api
- FinEventHub.Aggregation.Api

using Visual Studio or

```bash
dotnet run
```

---

# API Endpoints

## Publish Events

```
POST /api/v1/events/batch
```

Returns

```
202 Accepted
```

---

## Query Daily Summary

```
GET /api/v1/customers/{customerId}/daily-summary?date=YYYY-MM-DD&currency=TRY
```

Possible responses

```
200 OK
400 BadRequest
404 NotFound
```

---

# Validation

Incoming batches are validated using FluentValidation.

Validation rules include:

- Valid UUID
- CustomerId length
- Credit / Debit type
- Positive amount
- Currency format
- UTC date validation

If any event inside the batch is invalid, the whole batch is rejected.

---

# Idempotency

Duplicate events are prevented by storing processed EventIds in the database.

Before updating DailySummary, the Aggregation Service checks whether the event has already been processed.

Both inserting ProcessedEvents and updating DailySummary are executed inside the same database transaction to guarantee consistency.

---

# Retry & Dead Letter Queue

Retry is implemented using RabbitMQ Retry Queue.

Workflow

```
events
    |
Consumer
    |
Exception
    |
events.retry
    |
TTL expires
    |
events
```

Features

- Configurable retry count
- Configurable retry delay
- No infinite requeue loop
- Failed messages move to Dead Letter Queue
- Invalid or deserialization failures are moved directly to DLQ

---

# Backpressure

Backpressure is implemented using RabbitMQ QoS.

Configuration includes

- Configurable PrefetchCount
- Configurable ConsumerConcurrency
- Manual ACK
- Graceful shutdown using BackgroundService cancellation

---

# Database

PostgreSQL stores

- ProcessedEvents
- DailySummaries

Entity Framework Core migrations create the database schema automatically.

---

# Load Generator

A separate console application is included for load testing.

Features

- 100,000 synthetic events
- 10% duplicate events
- 100 customers
- Credit and Debit events
- Batch publishing

Example output

```
Total Sent         : 100000
Accepted           : 100000
Unique Events      : 90000
Duplicate Events   : 10000
Failed             : 0
Elapsed            : 00:25:35
```

---

# Running Tests

Run all tests

```bash
dotnet test
```

Current unit tests cover

- Duplicate event handling
- Credit aggregation
- Debit aggregation
- Existing summary updates
- Multiple customer aggregation

---

# Technical Decisions

- RabbitMQ selected as the message broker.
- PostgreSQL selected as the persistent database.
- Database-based idempotency instead of in-memory tracking.
- Retry implemented using RabbitMQ Retry Queue and TTL.
- Database transaction guarantees consistency between processed events and daily summaries.

---

# Known Limitations

The implementation focuses on the requirements of the technical assessment.

The following production features were intentionally left out:

- Authentication / Authorization
- Distributed tracing
- Monitoring dashboards
- Horizontal consumer scaling
- Full CI/CD pipeline

---

# Future Improvements

- OpenTelemetry
- Prometheus metrics
- Health Checks
- Testcontainers integration tests
- Publisher Confirms
- Outbox Pattern
- Rate Limiting

---

# AI Usage

AI-assisted tools were used during development for:

- Architecture discussions
- Code review
- Documentation drafting
- Test generation

All generated code was manually reviewed, adapted and verified before being included in the final solution.