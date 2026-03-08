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
| `GET` | `/Events` | Список всех событий |
| `GET` | `/Events/{id}` | Получение события по ID |
| `POST` | `/Events` | Создание нового события |
| `PUT` | `/Events/{id}` | Обновление события |
| `DELETE` | `/Events/{id}` | Удаление события |