FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EventGrok.Contracts/EventGrok.Contracts.csproj EventGrok.Contracts/
COPY EventGrok.Bookings/EventGrok.Bookings.Domain/EventGrok.Bookings.Domain.csproj EventGrok.Bookings/EventGrok.Bookings.Domain/
COPY EventGrok.Bookings/EventGrok.Bookings.Application/EventGrok.Bookings.Application.csproj EventGrok.Bookings/EventGrok.Bookings.Application/
COPY EventGrok.Bookings/EventGrok.Bookings.Infrastructure/EventGrok.Bookings.Infrastructure.csproj EventGrok.Bookings/EventGrok.Bookings.Infrastructure/
COPY EventGrok.Bookings/EventGrok.Bookings.Presentation/EventGrok.Bookings.Presentation.csproj EventGrok.Bookings/EventGrok.Bookings.Presentation/

RUN dotnet restore EventGrok.Bookings/EventGrok.Bookings.Presentation/EventGrok.Bookings.Presentation.csproj

COPY . .

RUN dotnet publish EventGrok.Bookings/EventGrok.Bookings.Presentation/EventGrok.Bookings.Presentation.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventGrok.Bookings.Presentation.dll"]
