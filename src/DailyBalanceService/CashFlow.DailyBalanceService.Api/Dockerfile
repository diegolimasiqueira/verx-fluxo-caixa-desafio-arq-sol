FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY build-output/balance .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CashFlow.DailyBalanceService.Api.dll"]
