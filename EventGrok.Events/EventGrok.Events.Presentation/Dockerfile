FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EventGrok.Contracts/EventGrok.Contracts.csproj EventGrok.Contracts/
COPY EventGrok.Events/EventGrok.Events.Domain/EventGrok.Events.Domain.csproj EventGrok.Events/EventGrok.Events.Domain/
COPY EventGrok.Events/EventGrok.Events.Application/EventGrok.Events.Application.csproj EventGrok.Events/EventGrok.Events.Application/
COPY EventGrok.Events/EventGrok.Events.Infrastructure/EventGrok.Events.Infrastructure.csproj EventGrok.Events/EventGrok.Events.Infrastructure/
COPY EventGrok.Events/EventGrok.Events.Presentation/EventGrok.Events.Presentation.csproj EventGrok.Events/EventGrok.Events.Presentation/

RUN dotnet restore EventGrok.Events/EventGrok.Events.Presentation/EventGrok.Events.Presentation.csproj

COPY . .

RUN dotnet publish EventGrok.Events/EventGrok.Events.Presentation/EventGrok.Events.Presentation.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventGrok.Events.Presentation.dll"]
