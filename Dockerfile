# ====================================================================
# ЭТАП 1: Базовый образ для выполнения (Runtime)
# ====================================================================
# Используй 9.0 или 8.0 в зависимости от версии твоего таргета
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

# В .NET 8/9 порт по умолчанию сменился на 8080 (запуск без root-прав)
EXPOSE 8080
EXPOSE 8081

# Переключаемся на встроенного непривилегированного пользователя для безопасности
USER $APP_UID

# ====================================================================
# ЭТАП 2: Сборка приложения (SDK)
# ====================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Сначала копируем ТОЛЬКО файлы проектов для кэширования dotnet restore!
# (Благодаря этому docker не будет заново качать NuGet пакеты, если ты просто меняешь .cs файлы)
COPY ["src/ArchivioLessicale.API/ArchivioLessicale.API.csproj", "src/ArchivioLessicale.API/"]
# Если есть другие проекты (Core, Domain), раскомментируй:
# COPY ["src/ArchivioLessicale.Core/ArchivioLessicale.Core.csproj", "src/ArchivioLessicale.Core/"]

# Восстанавливаем зависимости
RUN dotnet restore "src/ArchivioLessicale.API/ArchivioLessicale.API.csproj"

# Теперь копируем весь остальной исходный код
COPY . .

WORKDIR "/src/src/ArchivioLessicale.API"

# Собираем проект
RUN dotnet build "ArchivioLessicale.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# ====================================================================
# ЭТАП 3: Публикация артефактов
# ====================================================================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ArchivioLessicale.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ====================================================================
# ЭТАП 4: Финальный образ (только скомпилированные DLL + Runtime)
# ====================================================================
FROM base AS final
WORKDIR /app

# Копируем результат сборки из этапа publish
COPY --from=publish /app/publish .

# Точка входа в приложение
ENTRYPOINT ["dotnet", "ArchivioLessicale.API.dll"]
