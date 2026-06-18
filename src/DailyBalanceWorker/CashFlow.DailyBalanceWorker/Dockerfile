FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY build-output/worker .
ENTRYPOINT ["dotnet", "CashFlow.DailyBalanceWorker.dll"]
