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
  "detail": "Событие с id = 999 не найдено"
}
```
