# EventGrok API

ASP.NET Core Web API для управления событиями.

## Запуск

1. Запустите из корневой папки:
   ```bash
   dotnet run
   ```
2. Откройте Swagger UI: http://localhost:5263/swagger

## Тесты

Запустите из корневой папки:
```bash
dotnet test EventGrok.Tests/EventGrok.Tests.csproj
```

## API

| Метод | Путь | Описание |
| :--- | :--- | :--- |
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
- Имитация внешней проверки (2 секунды)
- Статус меняется на `Confirmed`, заполняется `ProcessedAt`

**Сценарий проверки фоновой обработки:**

1. Создайте событие: POST /events {body} → 201 Created, Location: /events/{eventGuid}
2. Создайте 5-10 броней подряд: POST /events/{eventGuid}/book
3. Скопируйте ID последней брони из ответа 202
4. GET /bookings/{bookingGuid} → можно успеть увидеть статус: Pending
5. Подождите несколько секунд
6. GET /bookings/{bookingGuid} → статус: Confirmed, ProcessedAt: заполнено