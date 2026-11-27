FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Accounting_Register_BE.csproj .
RUN dotnet restore "Accounting_Register_BE.csproj"
COPY . .
RUN dotnet publish "Accounting_Register_BE.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Accounting_Register_BE.dll"]
