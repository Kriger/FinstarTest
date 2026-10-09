# Task Management Service

REST API сервис для управления задачами с архитектурой Clean Architecture, Outbox-паттерном для гарантированной доставки событий, интеграцией с RabbitMQ, PostgreSQL и Redis.

## Архитектура

Проект из следующих слоёв:

```
FinstarTest.sln
|-- src/
│   |-- TaskManagement.Domain/          # Доменная модель
│   |-- TaskManagement.Application/     # Бизнес-логика
│   |-- TaskManagement.Infrastructure/  # Инфраструктура (EF Core, RabbitMQ, Redis)
│   |-- TaskManagement.Api/             # REST API (port 8080)
│   --- TaskManagement.Consumer/        # Consumer событий (фоновый сервис)
|-- tests/
│   |-- TaskManagement.Domain.Tests/
│   |-- TaskManagement.Application.Tests/
│   |-- TaskManagement.Infrastructure.Tests/
│   |-- TaskManagement.Api.Tests/
│   |-- TaskManagement.Integration.Tests/
│   --- TaskManagement.ProducerConsumer.Tests/
|-- docker-compose.yml
--- README.md
```

**Граф зависимостей:**

```
TaskManagement.Domain          (no dependencies)
     ^
TaskManagement.Application     (depends on Domain)
     ^
TaskManagement.Infrastructure  (depends on Application)
     ^
TaskManagement.Api             (depends on Application + Infrastructure)
TaskManagement.Consumer        (depends on Application + Infrastructure)
```

## Стек технологий

|       Компонент     |           Технология         |
|---------------------|------------------------------|
| Язык                | C# / .NET 10                 |
| ORM                 | Entity Framework Core        |
| База данных         | PostgreSQL                   |
| Кэш                 | Redis                        |
| Очередь сообщений   | RabbitMQ                     |
| Логирование         | Serilog                      |
| CORS                | Настроены AllowedOrigins     |
| Тесты               | xUnit, FluentAssertions, Moq |
| Контейнеризация     | Docker, Docker Compose       |
| Наблюдаемость       | OpenTelemetry                |

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/)
- [Docker Compose](https://docs.docker.com/compose/)

## Быстрый старт

### Локальный запуск

1. **Запустите инфраструктуру:**
   ```bash
   docker-compose up -d postgres rabbitmq redis
   ```

2. **Восстановите зависимости:**
   ```bash
   dotnet restore
   ```

3. **Запустите API:**
   ```bash
   dotnet run --project src/TaskManagement.Api
   ```
   API доступен по адресу: http://localhost:8080

4. **Swagger UI:** http://localhost:8080/swagger

5. **Запустите Consumer:**
   ```bash
   dotnet run --project src/TaskManagement.Consumer
   ```

### Запуск через Docker Compose

```bash
docker-compose up --build
```

Все сервисы запустятся автоматически:
- **PostgreSQL** — port 5432
- **RabbitMQ** — port 5672 (AMQP), 15672 (Management UI)
- **Redis** — port 6379
- **API** — port 8080
- **Consumer** — фоновый сервис

Management UI RabbitMQ: http://localhost:15672 (guest/guest)

## API Endpoints

### Задачи

| Метод    |      Endpoint       |       Описание        |
|----------|---------------------|-----------------------|
| `POST`   | `/api/v1/task`      | Создать задачу        |
| `GET`    | `/api/v1/task`      | Получить все задачи   |
| `GET`    | `/api/v1/task/{id}` | Получить задачу по ID |
| `PUT`    | `/api/v1/task/{id}` | Обновить задачу       |
| `DELETE` | `/api/v1/task/{id}` | Удалить задачу        |

### Примеры запросов

**Создать задачу:**
```bash
curl -X POST http://localhost:8080/api/v1/task \
  -H "Content-Type: application/json" \
  -d '{"title": "My Task", "description": "Task description"}'
```

**Получить все задачи (с пагинацией):**
```bash
curl "http://localhost:8080/api/v1/task?page=1&pageSize=20"
```

**Ответ:**
```json
{
  "page": 1,
  "pageSize": 20,
  "totalCount": 100,
  "totalPages": 5,
  "hasNextPage": true,
  "hasPreviousPage": false,
  "items": [...]
}
```

**Обновить задачу:**
```bash
curl -X PUT http://localhost:8080/api/v1/task/{id} \
  -H "Content-Type: application/json" \
  -d '{"title": "Updated Title", "status": "InProgress"}'
```

**Удалить задачу:**
```bash
curl -X DELETE http://localhost:8080/api/v1/task/{id}
```

## Структура проектов

### Domain (`TaskManagement.Domain`)
Доменная модель без зависимостей от внешних библиотек:
- `Models/Task.cs` — сущность задачи с валидацией, soft delete, optimistic concurrency (RowVersion)
- `Enums/TaskStatus.cs` — статусы (New, InProgress, Completed)
- `Events/TaskEvent.cs` — базовый класс событий + `TaskCreatedEvent`, `TaskUpdatedEvent`, `TaskDeletedEvent`
- `Outbox/OutboxMessage.cs` — сообщение Outbox-паттерна со статусами (Pending, Sending, Sent, Failed) и retry-логикой (MaxRetryCount: 5)

### Application (`TaskManagement.Application`)
Бизнес-логика и интерфейсы:
- `Services/TaskService.cs` — CRUD операции с валидацией, Outbox-паттерн, Redis кэширование
- `DTOs/` — объекты передачи данных (CreateTaskDto, UpdateTaskDto, TaskFilterDto, PaginatedResult<T>)
- `Interfaces/` — интерфейсы (ITaskRepository, IOutboxRepository, IEventPublisher, IUnitOfWork, IUserContext)
- `Configuration/` — расширение для регистрации в DI, CachingSettings
- `Mapping/ITaskMapper.cs` — маппинг сущностей в DTO
- `Exceptions/` — кастомные исключения (TaskNotFoundException)
- `Validation/` — FluentValidation валидаторы (CreateTaskValidator, UpdateTaskValidator)
- JWT аутентификация (System.IdentityModel.Tokens.Jwt)
- Memory caching (Microsoft.Extensions.Caching.Memory)

### Infrastructure (`TaskManagement.Infrastructure`)
Реализации внешних зависимостей:
- `Data/AppDbContext.cs` — EF Core контекст
- `Repositories/TaskRepository.cs` — репозиторий задач
- `Repositories/OutboxRepository.cs` — репозиторий Outbox с DbCommandExecutor
- `Messaging/RabbitMqEventPublisher.cs` — публикация событий в RabbitMQ
- `Messaging/RabbitMqConnectionFactory.cs` — фабрика соединений с Retry (3 попытки, exponential backoff) + Circuit Breaker (50% failure ratio, 30s break duration)
- `Messaging/RabbitMqServiceCollectionExtensions.cs` — регистрация RabbitMQ в DI (connection pooling)
- `Caching/` — Redis кэширование (StackExchange.Redis)
- `Logging/` — кастомное логирование

### Api (`TaskManagement.Api`)
REST API контроллеры:
- `Controllers/TaskController.cs` — CRUD endpoints
- `Program.cs` — конфигурация DI, Swagger, Health Checks, Exception Handling
- `Middleware/RateLimitingMiddleware.cs` — Rate Limiting (sliding window алгоритм)
- `Filters/ValidationActionFilter.cs` — валидация DTO
- `Services/OutboxBackgroundService.cs` — фоновый сервис для публикации Outbox-сообщений (batch processing, retry)
- `Services/HttpContextUserContext.cs` — контекст пользователя
- `Swagger/SwaggerExamples.cs` — примеры для Swagger

### Consumer (`TaskManagement.Consumer`)
Фоновый сервис, слушающий очередь RabbitMQ и логирующий события.

## Тестирование

```bash
# Запустить все тесты
dotnet test

# Запустить только unit-тесты Application
dotnet test tests/TaskManagement.Application.Tests

# Integration tests (требует Docker для Testcontainers)
dotnet test tests/TaskManagement.Integration.Tests
```

**Структура тестов:**

|                Проект                   |             Описание           |
|-----------------------------------------|--------------------------------|
| `TaskManagement.Domain.Tests`           | Unit-тесты доменной модели     |
| `TaskManagement.Application.Tests`      | Unit-тесты бизнес-логики       |
| `TaskManagement.Infrastructure.Tests`   | Unit-тесты инфраструктуры      |
| `TaskManagement.Api.Tests`              | Unit-тесты API                 |
| `TaskManagement.Integration.Tests`      | Integration-тесты              |
| `TaskManagement.ProducerConsumer.Tests` | Тесты Producer/Consumer логики |

## Health Checks

API предоставляет endpoints для проверок здоровья сервиса:
- `GET /health` - Проверка всех зависимостей

Возвращаемый статус:
- `200 OK` — все зависимости доступны
- `503 Service Unavailable` — одна или несколько зависимостей недоступны

## Exception Handling

API использует middleware для обработки исключений. Все ошибки возвращаются в структурированном формате:

```json
{
  "error": "Task not found",
  "statusCode": 404,
  "traceId": "abc-123-def"
}
```

Поддерживаемые коды ответов:
- `400 Bad Request` — невалидный аргумент
- `404 Not Found` — ресурс не найден
- `429 Too Many Requests` — превышен лимит запросов
- `500 Internal Server Error` — внутренняя ошибка

## Валидация

Используется FluentValidation вместо DataAnnotations. Правила валидации находятся в `src/TaskManagement.Application/Validation/`:

- `CreateTaskValidator` — валидация создания задачи
- `UpdateTaskValidator` — валидация обновления задачи

При ошибке валидации возвращается:
```json
{
  "errors": [
    { "field": "Title", "message": "Title is required." }
  ]
}
```

## Наблюдаемость

### API Versioning
API поддерживает версионирование тремя способами:
- **URL-сегмент:** `/api/v1/task`
- **Заголовок:** `X-Api-Version: 1.0`
- **Query-строка:** `?v=1.0`

По умолчанию используется версия `1.0`. Для добавления новой версии создайте новый контроллер с атрибутом `[ApiVersion("2.0")]`.

### Rate Limiting
API имеет встроенное ограничение частоты запросов через middleware (sliding window алгоритм):
- **Лимит:** 100 запросов в минуту на IP-адрес (настраивается через `RateLimitConfig`)
- **Заголовки ответа:**
  - `X-RateLimit-Limit` — максимальное количество запросов
  - `X-RateLimit-Remaining` — оставшиеся запросы
  - `Retry-After` — время ожидания (секунды) при превышении лимита
- **Код ответа:** 429 Too Many Requests
- **Защита от IP spoofing:** проверка заголовка `X-Forwarded-For`
- **Автоматическая очистка:** устаревшие записи удаляются каждые 30 секунд

Настройки в `appsettings.json`:
```json
"RateLimiting": {
  "IpRules": [
    { "Ip": "192.168.1.100", "Rules": "fixedByIp" }
  ],
  "GeneralRules": {
    "Period": "1m",
    "Limit": 100
  }
}
```

### CORS
Настроены разрешённые источники:
- `http://localhost:3000`
- `http://localhost:5173`

### Логирование
Serilog пишет логи в консоль и файлы:
- `logs/api-.log` — API (ежедневное ротирование)
- `logs/producer-.log` — Producer
- `logs/consumer-.log` — Consumer

Уровни логирования:
- `Microsoft` -> Warning
- `Microsoft.EntityFrameworkCore` -> Information
- Application -> Information

### EF Core Migrations
Миграции находятся в `src/TaskManagement.Infrastructure/Data/Migrations/`:
- `20241006000000_InitialCreate.cs` — начальная миграция
- `AppDbContextModelSnapshot.cs` — снимок модели

Миграции применяются автоматически при запуске в режиме Development.

Для создания новой миграции:
```bash
dotnet ef migrations add MigrationName --project src/TaskManagement.Infrastructure --startup-project src/TaskManagement.Api
```

## Outbox Pattern

Для гарантированной доставки событий используется Outbox-паттерн:

### Архитектура
1. **TaskService** — при создании/обновлении/удалении задачи создаётся `OutboxMessage` в той же транзакции
2. **OutboxBackgroundService** — фоновый сервис опрашивает таблицу Outbox каждые 5 секунд
3. **RabbitMqEventPublisher** — публикует событие в RabbitMQ

### Жизненный цикл сообщения
```
Pending → Sending → Sent (успех)
Pending → Sending → Pending (ошибка, retry)
Sending → Failed (исчерпаны попытки)
```

### Параметры OutboxBackgroundService
- **Batch size:** 10 сообщений за итерацию
- **Polling interval:** 5 секунд
- **Retry delay:** 10 секунд с exponential backoff (10s * 2^RetryCount)
- **Max retry count:** 5 попыток

### Статусы OutboxMessage
| Статус     | Описание                           |
|------------|------------------------------------|
| `Pending`  | Ожидает отправки                  |
| `Sending`  | Отправляется в данный момент       |
| `Sent`     | Отправлено успешно                |
| `Failed`   | Отправка не удалась (max retries) |

### Retry Policy
RabbitMqEventPublisher использует Polly для повторных попыток через `ResiliencePipeline`:
- **3 попытки** с exponential backoff
- **Начальная задержка:** 1 секунда
- **Логирование** каждой неудачной попытки

### Circuit Breaker
RabbitMqConnectionFactory использует Circuit Breaker для защиты от каскадных отказов:
- **Порог срабатывания:** 50% failure ratio при minimum throughput >= 20 запросов
- **Длительность circuit open:** 30 секунд
- **Half-open state:** после 30 секунд позволяет пробный запрос
- **Состояния:** Closed -> Open -> Half-Open -> Closed (если пробный запрос успешен)
- **Логирование:** все переходы состояний логируются в Serilog

### Трассировка (OpenTelemetry)
OpenTelemetry настроен с OTLP экспортером:
- ASP.NET Core instrumentation
- HTTP client instrumentation
- EF Core instrumentation

Для отправки трейсов в Jaeger/Zipkin настройте переменную окружения `Otlp:Endpoint`:
```bash
export Otlp__Endpoint=http://localhost:4317
```

## Настройки

### RabbitMQ

Все сервисы конфигурируются через `appsettings.json`:

**API (connection string для DI):**
```json
"ConnectionStrings": {
  "RabbitMq": "amqp://guest:guest@localhost:5672"
}
```

Регистрация через `AddRabbitMq(connectionString)`:
- **Singleton** `RabbitMqConnectionFactory` — фабрика соединений
- **Singleton** `IConnection` — пул соединений (переиспользуется)
- **Scoped** `RabbitMqEventPublisher` — новый канал для каждой операции

**Consumer:**
```json
"RabbitMq": {
  "HostName": "localhost",
  "Port": 5672,
  "VirtualHost": "/",
  "UserName": "guest",
  "Password": "guest",
  "QueueName": "task_events"
}
```

**Exchange и очереди:**
- **Exchange:** `task_events` (direct, durable)
- **Queue:** `task_events` (durable)
- **Routing keys:** `task_created`, `task_updated`, `task_deleted`

## Конфигурация

Основные настройки в `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=taskmanagement;Username=postgres;Password=postgres",
    "RabbitMq": "amqp://guest:guest@localhost:5672",
    "Redis": "localhost:6379"
  },
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:5173"
  ]
}
```

## Лицензия

MIT
