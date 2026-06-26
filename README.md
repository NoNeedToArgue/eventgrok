# EventGrok API

ASP.NET Core Web API для управления событиями.

## Структура проекта

Clean Architecture:

| Слой | Назначение |
|---|---|
| `EventGrok.Domain` | Сущности (`Event`, `Booking`), доменные правила и исключения |
| `EventGrok.Application` | Сервисы, DTO, интерфейсы репозиториев (порты), фоновые сервисы |
| `EventGrok.Infrastructure` | Реализация репозиториев, `AppDbContext`, миграции EF Core |
| `EventGrok.Presentation` | Контроллеры, middleware, `Program.cs` (composition root) |
| `EventGrok.Tests` | Юнит-тесты (InMemory EF Core) |
| `EventGrok.IntegrationTests` | Интеграционные тесты (PostgreSQL в Docker через Testcontainers) |
| `EventGrok.ApiTests` | E2E-тесты API (WebApplicationFactory + Testcontainers) |

## Запуск

1. Для работы приложения требуется PostgreSQL. При использовании локальной БД настройте строку подключения в `appsettings.json`.

   Docker. Запустите из корневой папки:
   ```bash
   docker compose up -d
   ```
   
   Схема БД управляется миграциями EF Core. При запуске применяется `Migrate()`.

2. Запустите из корневой папки:
   ```bash
   dotnet run --project EventGrok.Presentation
   ```
   
3. Откройте Swagger UI: http://localhost:5263/swagger

## Миграции

Создание:
```bash
dotnet ef migrations add <Name> --project EventGrok.Infrastructure --startup-project EventGrok.Presentation
```
Применение:
```bash
dotnet ef database update --project EventGrok.Infrastructure --startup-project EventGrok.Presentation
```

## Тесты

**Юнит-тесты используют EF Core InMemory Provider.**

Запустите из корневой папки:
```bash
dotnet test EventGrok.Tests/EventGrok.Tests.csproj
```

**Интеграционные тесты используют PostgreSQL в Docker. Требуется запущенный Docker Desktop.**

Запустите из корневой папки:
```bash
dotnet test EventGrok.IntegrationTests/EventGrok.IntegrationTests.csproj
```

**E2E-тесты API используют `WebApplicationFactory` и PostgreSQL в Docker. Требуется запущенный Docker Desktop.**

Запустите из корневой папки:
```bash
dotnet test EventGrok.ApiTests/EventGrok.ApiTests.csproj
```

## Аутентификация

### Ролевая модель

| Роль | Доступ |
| --- | --- |
| `User` | Регистрация, логин, просмотр событий, бронирование, отмена своей брони |
| `Admin` | Все действия `User` + CRUD событий, отмена любой брони |

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

| Метод | Путь | Описание |
| :--- | :--- | :--- |
| `POST` | `/auth/register` | Регистрация пользователя |
| `POST` | `/auth/login` | Получение JWT-токена |
| `GET` | `/events` | Список всех событий |
| `GET` | `/events/{id}` | Получение события по ID |
| `POST` | `/events` | Создание нового события |
| `PUT` | `/events/{id}` | Обновление события |
| `DELETE` | `/events/{id}` | Удаление события |
| `POST` | `/events/{id}/book` | Создание брони для события |
| `GET` | `/bookings/{id}` | Получение брони по ID |

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

### Фоновая обработка

Бронирования со статусом `Pending` автоматически обрабатываются фоновым сервисом:
- Опрос каждые 2 секунды
- Статус меняется на `Confirmed`, заполняется `ProcessedAt`

### Синхронизация

**Защита от овербукинга:** 

При одновременных запросах создаётся ровно столько броней, сколько доступно мест (`AvailableSeats`). Остальные получают `409 Conflict`. 

Запустите из корневой папки:
```bash
dotnet test EventGrok.Tests/EventGrok.Tests.csproj --filter "Type=Concurrency"
```