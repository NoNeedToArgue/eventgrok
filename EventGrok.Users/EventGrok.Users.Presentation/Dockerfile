FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EventGrok.Contracts/EventGrok.Contracts.csproj EventGrok.Contracts/
COPY EventGrok.Users/EventGrok.Users.Domain/EventGrok.Users.Domain.csproj EventGrok.Users/EventGrok.Users.Domain/
COPY EventGrok.Users/EventGrok.Users.Application/EventGrok.Users.Application.csproj EventGrok.Users/EventGrok.Users.Application/
COPY EventGrok.Users/EventGrok.Users.Infrastructure/EventGrok.Users.Infrastructure.csproj EventGrok.Users/EventGrok.Users.Infrastructure/
COPY EventGrok.Users/EventGrok.Users.Presentation/EventGrok.Users.Presentation.csproj EventGrok.Users/EventGrok.Users.Presentation/

RUN dotnet restore EventGrok.Users/EventGrok.Users.Presentation/EventGrok.Users.Presentation.csproj

COPY . .

RUN dotnet publish EventGrok.Users/EventGrok.Users.Presentation/EventGrok.Users.Presentation.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventGrok.Users.Presentation.dll"]
