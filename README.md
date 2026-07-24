# EventGrok API

Микросервисная система управления событиями на базе ASP.NET Core Web API.

## Структура проекта

| Сервис / Проект | Назначение |
|---|---|
| `EventGrok.Contracts` | Общие DTO для межсервисного взаимодействия и имена топиков Kafka |
| `EventGrok.Users.*` | Управление пользователями, выдача JWT-токенов |
| `EventGrok.Events.*` | CRUD событий (Admin), подписка на Kafka для списания мест |
| `EventGrok.Bookings.*` | Создание броней, публикация событий в Kafka |
| `*.Tests` | Юнит-тесты (InMemory EF Core) |
| `*.IntegrationTests` | Интеграционные тесты (PostgreSQL в Docker через Testcontainers) |
| `*.ApiTests` | E2E-тесты API (WebApplicationFactory + Testcontainers) |

Clean Architecture: каждый сервис имеет свои слои (`Domain`, `Application`, `Infrastructure`, `Presentation`) и изолированную базу данных.

## Запуск

1. Запустите из корневой папки:
   
   ```bash
   docker-compose up -d
   ```

2. Сервисы будут доступны по адресам:

    - Users API: http://localhost:5001/swagger
    - Events API: http://localhost:5002/swagger
    - Bookings API: http://localhost:5003/swagger

## Тесты

Каждый сервис содержит свои наборы тестов:

  - Юнит-тесты используют EF Core InMemory Provider
  - Интеграционные тесты используют PostgreSQL в Docker
  - E2E-тесты API используют `WebApplicationFactory` и PostgreSQL в Docker

Запустите из корневой папки:
```bash
dotnet test -m:1
```

## Аутентификация

### Ролевая модель

| Роль | Доступ |
| --- | --- |
| `User` | Регистрация, логин, просмотр событий, бронирование, отмена своей брони |
| `Admin` | Все действия `User` + CRUD событий, отмена любой брони |

Сервис `Users` выдаёт JWT-токен. Сервисы `Events` и `Bookings` его валидируют.

Защищённые эндпоинты возвращают `401` без токена и `403` при недостаточных правах.

### Получение JWT-токена через Swagger

1. Зарегистрируйтесь: `POST /auth/register`
2. Получите токен: `POST /auth/login`
3. Нажмите **Authorize** в Swagger UI
4. Введите токен

### Конфигурация JWT

Для демонстрации секрет хранится в `appsettings.json`:

```json
{
  "JwtSettings": {
    "Secret": "NotSoSecretKey_ONLY_FOR_DEMONSTRATION!",
    "Issuer": "EventGrok",
    "Audience": "EventGrokClients",
    "LifetimeMinutes": 60
  }
}
```

В продакшне используйте безопасное значение (минимум 32 символа), применяйте user secrets или переменные окружения.

## API

| Сервис | Основные эндпоинты |
|---|---|
| **Users** | `POST /auth/register`, `POST /auth/login` |
| **Events** | `GET /events`, `GET /events/{id}`, `GET /events/top`, `POST/PUT/DELETE /events` (требуют роль `Admin`) |
| **Bookings** | `POST /bookings`, `GET /bookings/{id}`, `DELETE /bookings/{id}` |

### Параметры фильтрации для `GET /events`

| Параметр | Тип | Обязательный | Описание |
|----------|-----|-------------|----------|
| `title` | string | ❌ | Фильтр по названию (регистронезависимый, частичное совпадение) |
| `from` | DateTime | ❌ | Фильтр по дате начала: `StartAt >= from` |
| `to` | DateTime | ❌ | Фильтр по дате окончания: `EndAt <= to` |
| `page` | int | ❌ | Номер страницы (по умолчанию: 1) |
| `pageSize` | int | ❌ | Элементов на странице (по умолчанию: 10) |

**Пример запроса:**
```http
GET /events?title=концерт&from=2026-01-01&page=2&pageSize=10
```

**Формат успешного ответа:**
```json
{
  "items": [...],
  "totalCount": 50,
  "page": 2,
  "pageSize": 10,
  "totalPages": 5
}
```

**Формат ответа при ошибках:**
```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Not Found",
  "status": 404,
  "detail": "Событие с id = {guid} не найдено"
}
```

### Модель Event

| Поле | Тип | Описание |
| --- | --- | --- |
| `Id` | Guid | Уникальный идентификатор |
| `Title` | string | Название события |
| `Description` | string | Описание |
| `StartAt` | DateTime | Дата начала |
| `EndAt` | DateTime | Дата окончания |
| `TotalSeats` | int | Общее количество мест (>0) |
| `AvailableSeats` | int | Доступные места (при создании = TotalSeats) |

### Модель Booking

| Поле | Тип | Описание |
| --- | --- | --- |
| `Id` | Guid | Уникальный идентификатор брони |
| `EventId` | Guid | ID события |
| `Status` | BookingStatus | Текущий статус |
| `CreatedAt` | DateTime | Дата создания |
| `ProcessedAt` | DateTime? | Дата обработки (заполняется после обработки) |

**Статусы (BookingStatus)**

| Статус | Описание |
| --- | --- |
| `Pending` | Ожидает обработки |
| `Confirmed` | Подтверждена |
| `Rejected` | Отклонена |
| `Cancelled` | Отменена |

## Асинхронное взаимодействие (Kafka)

1. При создании брони сервис **Bookings** сохраняет её со статусом `Pending` и немедленно возвращает ответ клиенту.
2. Фоновый сервис (Background Service) обрабатывает новые брони, подтверждает их и публикует событие `BookingConfirmed` в топик Kafka.
3. Сервис **Events** подписан на этот топик. При получении сообщения он находит событие по `EventId` и уменьшает `AvailableSeats`.

## Кеширование (Redis)

Сервис **Events** использует Redis (Cache-Aside) для двух сценариев:

| Сценарий | Ключ | TTL | Инвалидация |
| --- | --- | --- | --- |
| Получение события по ID | `event:{id}` | 5 мин | `PUT /events/{id}`, `DELETE /events/{id}`, `BookingConfirmed` |
| Топ-10 по проценту зарезервированных мест | `events:top10` | 10 мин | Нет, только TTL |

### Стратегия

- **Чтение:** проверка кеша → промах → запрос в БД → запись в кеш.
- **Запись:** сначала сохранение в БД, затем удаление ключа из кеша (invalidate-on-write).
- **Топ-10:** не инвалидируется при каждом бронировании - рейтинг допускает задержку, TTL достаточно.

### Поведение при недоступности Redis

- `AbortOnConnectFail = false` - сервис стартует без Redis.
- Ошибки Redis логируются, но не пробрасываются клиенту.
- Запрос деградирует в PostgreSQL.

### Конфигурация

TTL вынесены в `appsettings.json`:

```json
"Cache": {
  "EventTtlMinutes": 5,
  "TopEventsTtlMinutes": 10
}
```

---