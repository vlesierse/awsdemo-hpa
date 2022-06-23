FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["orderservice/orderservice.csproj", "orderservice/orderservice.csproj"]
COPY ["infrastructure/infrastructure.csproj", "infrastructure/infrastructure.csproj"]
RUN dotnet restore orderservice/orderservice.csproj
COPY . .

WORKDIR /src/orderservice
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "orderservice.dll"]
