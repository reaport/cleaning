FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем файлы проекта и восстанавливаем зависимости
COPY ./CleaningService/*.csproj ./
RUN dotnet restore

# Копируем все файлы и собираем приложение
COPY ./CleaningService/ ./
RUN dotnet publish -c Release -o out ./CleaningService.csproj

# Собираем финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "CleaningService.dll"]
