FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

COPY ["src/orderservice/orderservice.csproj", "src/orderservice/orderservice.csproj"]
COPY ["src/infrastructure/infrastructure.csproj", "src/infrastructure/infrastructure.csproj"]
RUN dotnet restore src/orderservice/orderservice.csproj
COPY . .

WORKDIR /src/src/orderservice
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "orderservice.dll"]
