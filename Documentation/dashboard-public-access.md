# Opening the server and its dashboard to the internet (DuckDNS + HTTPS)

The dedicated server listens on one port number (default `7777`):

- **UDP 7777**: the game.
- **TCP 7777**: the web dashboard, `http(s)://<address>:7777/dashboard`.

## 1. Reach the server from outside

1. **DuckDNS:** create a name (for example `myserver.duckdns.org`) and keep the DuckDNS updater running, so the name follows your public IP.
2. **Router:** forward **UDP 7777** and **TCP 7777** to the PC that runs the server (fixed LAN IP for that PC).
3. **Firewall on the server PC:**
   - Windows: allow `Spacebox.exe` when Windows asks (also on public networks), or add inbound rules for TCP and UDP 7777.
   - Linux: `sudo ufw allow 7777` (if ufw is used).
4. **Check for CGNAT:** if the WAN IP shown in your router differs from the IP DuckDNS shows, your provider uses CGNAT and port forwarding cannot work. Ask the provider for a public IP or use a VPN/tunnel service.
5. **Inside your own network**, the DuckDNS name may not work if the router has no NAT loopback ("hairpinning"). Use the LAN address there: `http://192.168.x.x:7777/dashboard`.

## 2. Turn on HTTPS (needed for internet access)

Without HTTPS the password and the login cookie travel unencrypted.

### Automatic (recommended)

Set `PublicAddress` (the name or public IP people use) in `serverconfig.json`. The server picks the way to prove the address to Let's Encrypt:

| Address | Also set | How it is checked | Extra port |
|---|---|---|---|
| `*.duckdns.org` | `DuckDnsToken` | DuckDNS TXT record; the server also keeps the name on your current IP | none |
| any domain on Cloudflare | `CloudflareApiToken` (template "Edit zone DNS") | Cloudflare TXT record | none |
| any other name (No-IP, Dynu, own domain) | – | file on TCP `HttpsChallengePort` (80) | TCP 80 |
| public IP only | – | file on TCP 80, 6-day certificates | TCP 80 |

```json
"PublicAddress": "myserver.duckdns.org",
"DuckDnsToken": "<your DuckDNS token>",
"LetsEncryptEmail": "you@example.com"
```

If port 80 is blocked or needs root (Linux), forward external 80 to another internal port and set `HttpsChallengePort`. `HttpsAutomatic: false` turns the automatic mode off. The certificate is stored in `dashboard/certificates/<address>/`, checked every 12 hours and renewed when a third of its lifetime is left (30 days for 90-day certificates, about 2 days for IP certificates). Failures are logged and retried after an hour. `LetsEncryptStaging: true` uses the Let's Encrypt test system (certificates not trusted by browsers) for trying the setup without hitting rate limits. The guide `Spacebox Server Guide.pdf` in the game folder explains the same steps.

### Manual (your own certificate)

Use this only if you do not use DuckDNS or want your own certificate files. A manual `DashboardCertificateFile` turns the automatic mode off.

### Linux (acme.sh)

```sh
curl https://get.acme.sh | sh -s email=you@example.com
export DuckDNS_Token="<your DuckDNS token>"
~/.acme.sh/acme.sh --issue --server letsencrypt --dns dns_duckdns -d myserver.duckdns.org
mkdir -p ~/spacebox/certs
~/.acme.sh/acme.sh --install-cert -d myserver.duckdns.org \
  --key-file ~/spacebox/certs/privkey.pem \
  --fullchain-file ~/spacebox/certs/fullchain.pem
```

acme.sh installs a cron job that renews the certificate and copies it to the same files again.

### Windows (Posh-ACME, PowerShell)

```powershell
Install-Module -Name Posh-ACME -Scope CurrentUser
$token = ConvertTo-SecureString '<your DuckDNS token>' -AsPlainText -Force
New-PACertificate 'myserver.duckdns.org' -AcceptTOS -Contact 'you@example.com' -Plugin DuckDNS -PluginArgs @{ DuckToken = $token; DuckDomain = 'myserver' }
(Get-PACertificate).FullChainFile   # path of fullchain.cer
(Get-PACertificate).KeyFile         # path of cert.key
```

Renew with `Submit-Renewal` in a scheduled task (for example daily). Point the config at the files it prints (or copy them next to the server after each renewal).

### Server config

In `serverconfig.json` next to the server executable:

```json
"DashboardCertificateFile": "certs/fullchain.pem",
"DashboardCertificateKeyFile": "certs/privkey.pem",
"DashboardCertificatePassword": ""
```

- Relative paths start at the server folder; absolute paths work too.
- A `.pfx`/`.p12` file is also accepted: put it in `DashboardCertificateFile`, its password in `DashboardCertificatePassword`, and leave the key file empty.
- The server user must be able to read both files.

On start the server logs `HTTPS certificate loaded: ..., valid until ...` and the address to open: `https://myserver.duckdns.org:7777/dashboard`.

## How the server behaves with HTTPS

- HTTPS and plain HTTP share the same port.
- Plain HTTP is still accepted from your local network (LAN, localhost).
- From the internet, plain HTTP is redirected to HTTPS; requests that change something are refused.
- Login cookies made over HTTPS are marked `Secure`.
- Renewed certificate files are picked up within a minute, without a restart. 14 days before expiry the server logs a warning.
- If the files are missing or broken, the server logs the reason and the dashboard stays HTTP only.

## Security checklist

- Use a strong dashboard password; the dashboard locks an address for 5 minutes after 5 wrong attempts.
- Keep `dashboard/admin.json` and the certificate key private. Delete `dashboard/admin.json` to register a new admin (a new setup key is written on the next start).
- If you do not need the dashboard from outside, do not forward TCP 7777, or set `"DashboardEnabled": false`.
