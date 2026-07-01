# Stage 1: Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SmartLocker.API/SmartLocker.API.csproj", "SmartLocker.API/"]
RUN dotnet restore "SmartLocker.API/SmartLocker.API.csproj"
COPY . .
WORKDIR "/src/SmartLocker.API"
RUN dotnet build "SmartLocker.API.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "SmartLocker.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartLocker.API.dll"]
