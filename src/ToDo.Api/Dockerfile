# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/ToDo.Api/ToDo.Api.csproj", "src/ToDo.Api/"]
COPY ["src/ToDo.Application/ToDo.Application.csproj", "src/ToDo.Application/"]
COPY ["src/ToDo.Domain/ToDo.Domain.csproj", "src/ToDo.Domain/"]
COPY ["src/ToDo.Persistence/ToDo.Persistence.csproj", "src/ToDo.Persistence/"]
RUN dotnet restore "./src/ToDo.Api/ToDo.Api.csproj"
COPY . .
WORKDIR "/src/src/ToDo.Api"
RUN dotnet build "./ToDo.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ToDo.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ToDo.Api.dll"]