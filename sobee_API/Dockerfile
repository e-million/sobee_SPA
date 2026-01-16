FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY sobee_API/sobee_API/sobee_API.csproj sobee_API/sobee_API/
COPY sobee_API/Sobee.Domain/Sobee.Domain.csproj sobee_API/Sobee.Domain/
RUN dotnet restore sobee_API/sobee_API/sobee_API.csproj

COPY sobee_API/ sobee_API/
WORKDIR /src/sobee_API/sobee_API
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN addgroup --system app && adduser --system --ingroup app app

ENV ASPNETCORE_URLS="https://+:7058;http://+:5029" \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 7058
EXPOSE 5029

COPY --from=build --chown=app:app /app/publish .
USER app

ENTRYPOINT ["dotnet", "sobee_API.dll"]
