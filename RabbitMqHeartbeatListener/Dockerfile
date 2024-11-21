#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
# Install tzdata for timezone support
RUN apk add --no-cache tzdata
# Set a default timezone (optional)
ENV TZ=Australia/Brisbane
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RabbitMqHeartbeatListener/RabbitMqHeartbeatListener.csproj", "RabbitMqHeartbeatListener/"]
RUN dotnet restore "./RabbitMqHeartbeatListener/RabbitMqHeartbeatListener.csproj"
COPY . .
WORKDIR "/src/RabbitMqHeartbeatListener"
RUN dotnet build "./RabbitMqHeartbeatListener.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./RabbitMqHeartbeatListener.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir /data && chown -R app:app /data && chmod 777 /data
#USER app
ENTRYPOINT ["dotnet", "RabbitMqHeartbeatListener.dll"]