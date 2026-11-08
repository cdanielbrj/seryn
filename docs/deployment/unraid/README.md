# Unraid deployment

The template [`seryn.xml`](seryn.xml) uses `ghcr.io/cdanielbrj/seryn:latest` and persists application data in `/mnt/user/appdata/seryn`.

Add the template repository URL in Unraid:

```text
https://raw.githubusercontent.com/cdanielbrj/seryn/main/docs/deployment/unraid/seryn.xml
```

Before starting the container, create the appdata directory for the unprivileged ASP.NET runtime user:

```sh
mkdir -p /mnt/user/appdata/seryn
chown 1654:1654 /mnt/user/appdata/seryn
chmod 700 /mnt/user/appdata/seryn
```

Do not use privileged mode or permissions such as `777`. Set a unique administrator password before the first start. For reverse-proxy access, enable HTTPS enforcement and configure the exact trusted proxy address. See [configuration](../configuration.md) for the security model.
