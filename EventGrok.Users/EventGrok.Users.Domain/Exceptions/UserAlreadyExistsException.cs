namespace EventGrok.Users.Domain.Exceptions;

public class UserAlreadyExistsException(string login) 
    : Exception($"Пользователь с логином '{login}' уже существует");