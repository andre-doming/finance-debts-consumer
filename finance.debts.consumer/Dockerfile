FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore finance-debts-consumer/finance.debts.consumer/finance.debts.consumer.csproj

RUN dotnet publish finance-debts-consumer/finance.debts.consumer/finance.debts.consumer.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "finance.debts.consumer.dll"]