# EventGrok API

ASP.NET Core Web API для управления событиями.

## Запуск

1. Запустите из корневой папки:
   ```bash
   dotnet run
   ```
2. Откройте Swagger UI: http://localhost:5263/swagger

## API

| Метод | Путь | Описание |
| :--- | :--- | :--- |
| `GET` | `/events` | Список всех событий |
| `GET` | `/events/{id}` | Получение события по ID |
| `POST` | `/events` | Создание нового события |
| `PUT` | `/events/{id}` | Обновление события |
| `DELETE` | `/events/{id}` | Удаление события |