# Deployment

Seryn runs as one ASP.NET Core container. The same process serves the compiled frontend and `/api/v1`; Node is used only during the image build. SQLite, sessions, and Data Protection keys persist under `/data`.

- [Docker operations](docker.md)
- [Configuration](configuration.md)
- [Unraid](unraid/README.md)

The executable deployment contracts remain at the repository root: `Dockerfile`, `compose.yaml`, and `.env.example`. Documentation explains them but does not replace them.
