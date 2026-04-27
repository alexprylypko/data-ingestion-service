FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["TransactionApi/TransactionApi.csproj", "TransactionApi/"]
RUN dotnet restore "TransactionApi/TransactionApi.csproj"

COPY . .
WORKDIR "/src/TransactionApi"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TransactionApi.dll"]
