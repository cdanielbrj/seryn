# Configuration

Copy `.env.example` to `.env` for Compose-based installations. `.env` is local, ignored by Git, and excluded from the Docker build context.

| Variable | Purpose | Default |
| --- | --- | --- |
| `SERYN_ADMIN_USER` | Initial administrator name for a new database | `admin` |
| `SERYN_ADMIN_PASSWORD` | Initial 16–128 character password | none |
| `SERYN_ADMIN_PASSWORD_FILE` | Alternative secret-file path inside the container | none |
| `SERYN_BIND_ADDRESS` | Host address published by Compose | `127.0.0.1` |
| `SERYN_PORT` | Host port published by Compose | `8080` |
| `SERYN_ALLOWED_HOSTS` | ASP.NET Core allowed hosts | `localhost;127.0.0.1` |
| `SERYN_REQUIRE_HTTPS` | Reject API requests not observed as HTTPS | `false` in local Compose |
| `SERYN_TRUSTED_PROXY` | Exact reverse-proxy IP trusted for forwarded headers | `127.0.0.1` |

Single-quote `.env` values containing `$`, `#`, or spaces to prevent Compose interpolation.

## Initial administrator

Provisioning runs only when SQLite has no administrator. An existing stored administrator always wins, including after restore. Changing or removing environment variables does not reset the stored password, sessions, or lockout state.

Use exactly one password source. When using `SERYN_ADMIN_PASSWORD_FILE`, leave `SERYN_ADMIN_PASSWORD` empty and mount the secret read-only. The file must be readable by container UID 1654, contain at most 1024 bytes, and contain only the password. Trailing LF or CRLF is removed; other whitespace remains part of the password.

There is no password recovery workflow in v0.1. Keep the credential in a password manager.

## Remote access

Remote access and iPhone installation should use an HTTPS reverse proxy administered by the operator. A typical configuration is:

```dotenv
SERYN_BIND_ADDRESS=0.0.0.0
SERYN_PORT=8080
SERYN_ALLOWED_HOSTS=seryn.example.local
SERYN_REQUIRE_HTTPS=true
SERYN_TRUSTED_PROXY=192.168.1.10
```

Restrict the published port to the proxy with a firewall. The proxy must preserve `Host`, overwrite `X-Forwarded-Proto` with `https`, and manage `X-Forwarded-For`. Configure the exact address observed by the container; never trust an arbitrary network.

Direct HTTP may be explicitly selected on a trusted LAN or private overlay network, but credentials then travel without transport encryption.
