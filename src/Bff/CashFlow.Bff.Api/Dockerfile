FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY build-output/bff .
EXPOSE 8080
ENTRYPOINT ["dotnet", "CashFlow.Bff.Api.dll"]
