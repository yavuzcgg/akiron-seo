# Multi-stage Dockerfile for .NET 10 Web API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["src/Presentation/AkironSeo.API/AkironSeo.API.csproj", "src/Presentation/AkironSeo.API/"]
COPY ["src/Infrastructure/AkironSeo.Infrastructure/AkironSeo.Infrastructure.csproj", "src/Infrastructure/AkironSeo.Infrastructure/"]
COPY ["src/Core/AkironSeo.Application/AkironSeo.Application.csproj", "src/Core/AkironSeo.Application/"]
COPY ["src/Core/AkironSeo.Domain/AkironSeo.Domain.csproj", "src/Core/AkironSeo.Domain/"]

RUN dotnet restore "src/Presentation/AkironSeo.API/AkironSeo.API.csproj"

# Copy source code and publish
COPY . .
WORKDIR "/src/src/Presentation/AkironSeo.API"
RUN dotnet publish "AkironSeo.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5248
ENV ASPNETCORE_URLS=http://+:5248
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AkironSeo.API.dll"]
