FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY sobee_API.sln ./
COPY sobee_API/sobee_API.csproj sobee_API/
COPY Sobee.Domain/Sobee.Domain.csproj Sobee.Domain/
RUN dotnet restore sobee_API/sobee_API.csproj

COPY . .
RUN dotnet publish sobee_API/sobee_API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 7058
EXPOSE 5029

COPY --from=build /app/publish .
USER app

ENTRYPOINT ["dotnet", "sobee_API.dll"]
