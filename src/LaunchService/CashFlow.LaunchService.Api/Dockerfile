FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY build-output/launch .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CashFlow.LaunchService.Api.dll"]
