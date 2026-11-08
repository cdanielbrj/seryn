# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM node:24-bookworm-slim AS web-deps
WORKDIR /workspace/src/web
COPY src/web/package*.json ./
RUN npm ci

FROM web-deps AS web-source
COPY src/web/ ./
COPY tests/web/ /workspace/tests/web/

FROM web-source AS web-test
RUN npm run lint && npm test && npm run build

FROM web-source AS web-build
RUN npm run build

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS server-build
WORKDIR /source
COPY global.json Directory.Build.props Seryn.slnx ./
COPY src/server/Seryn.Server.csproj src/server/
COPY tests/server/Seryn.Tests.csproj tests/server/
RUN dotnet restore Seryn.slnx
COPY src/server/ src/server/
COPY tests/server/ tests/server/

FROM server-build AS server-test
RUN dotnet test Seryn.slnx -c Release --no-restore

FROM server-build AS publish
COPY --from=web-build /workspace/src/web/dist/ src/server/wwwroot/
RUN dotnet publish src/server -c Release --no-restore -o /app/publish /p:UseAppHost=false

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
