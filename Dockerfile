# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM node:24-bookworm-slim AS web-deps
WORKDIR /web
COPY src/Seryn.Web/package*.json ./
RUN npm ci

FROM web-deps AS web-source
COPY src/Seryn.Web/ ./

FROM web-source AS web-test
RUN npm run lint && npm test && npm run build

FROM web-source AS web-build
RUN npm run build

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS server-build
WORKDIR /source
COPY global.json Directory.Build.props Seryn.slnx ./
COPY src/Seryn.Server/Seryn.Server.csproj src/Seryn.Server/
COPY tests/Seryn.Tests/Seryn.Tests.csproj tests/Seryn.Tests/
RUN dotnet restore Seryn.slnx
COPY src/Seryn.Server/ src/Seryn.Server/
COPY tests/Seryn.Tests/ tests/Seryn.Tests/

FROM server-build AS server-test
RUN dotnet test Seryn.slnx -c Release --no-restore

FROM server-build AS publish
COPY --from=web-build /web/dist/ src/Seryn.Server/wwwroot/
RUN dotnet publish src/Seryn.Server -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 Seryn__DataDirectory=/data
RUN mkdir -p /data && chown app:app /data && chmod 700 /data
COPY --from=publish /app/publish ./
USER app
EXPOSE 8080
VOLUME ["/data"]
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 CMD ["dotnet", "Seryn.Server.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "Seryn.Server.dll"]
