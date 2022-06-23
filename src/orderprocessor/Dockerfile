FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["src/orderprocessor/orderprocessor.csproj", "src/orderprocessor/orderprocessor.csproj"]
RUN dotnet restore src/orderprocessor/orderprocessor.csproj
COPY . .

WORKDIR /src/src/orderprocessor
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "orderprocessor.dll"]
