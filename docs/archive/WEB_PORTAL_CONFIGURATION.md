# Web Portal Configuration - Ports and SSL Certificates

**Deployment Mode:** Windows Service (Kestrel)  
**No IIS Required!**

---

## 🌐 Port Configuration

### Default Ports
```json
{
  "WebPortal": {
    "Kestrel": {
      "HttpPort": 8080,
      "HttpsPort": 8443,
      "EnableHttps": false
    }
  }
}
```

### Access URLs
- **HTTP:** http://server:8080
- **HTTPS:** https://server:8443 (when enabled)

### Change Ports via Config Tool

The Configuration Tool will have a "Web Portal" tab where you can:
1. Set HTTP port (1-65535)
2. Set HTTPS port (1-65535)
3. Enable/disable HTTPS
4. Browse for SSL certificate
5. Test certificate validity
6. Save and restart service

---

## 🔒 SSL Certificate Configuration

### Option 1: Via Configuration Tool (Recommended)

**Steps:**
1. Open Configuration Tool
2. Navigate to "Web Portal" tab
3. Check "Enable HTTPS"
4. Click "Browse" to select .pfx certificate
5. Enter certificate password
6. Click "Test Certificate" to validate
7. Click "Save Configuration"
8. Restart Web Portal service

### Option 2: Via appsettings.json (Manual)

```json
{
  "WebPortal": {
    "Kestrel": {
      "HttpPort": 80,        // Standard HTTP (requires admin)
      "HttpsPort": 443,      // Standard HTTPS (requires admin)
      "EnableHttps": true,
      "CertificatePath": "C:\\ProgramData\\ZLFileRelay\\certs\\server.pfx",
      "CertificatePassword": "your-cert-password"
    }
  }
}
```

**Then restart service:**
```powershell
Restart-Service ZLFileRelay.WebPortal
```

---

## 📜 Obtaining SSL Certificates

### Option 1: Internal CA (Recommended for OT)

If your organization has an internal Certificate Authority:

```powershell
# Request certificate from internal CA
# Export as .pfx with private key
# Copy to server
# Configure path in appsettings.json
```

### Option 2: Self-Signed Certificate (Development/Testing)

```powershell
# Create self-signed certificate
$cert = New-SelfSignedCertificate `
    -Subject "CN=zlfilerelay.yourcompany.local" `
    -DnsName "zlfilerelay", "zlfilerelay.yourcompany.local", "localhost" `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -NotAfter (Get-Date).AddYears(5) `
    -CertStoreLocation "Cert:\LocalMachine\My" `
    -FriendlyName "ZL File Relay Web Portal" `
    -KeyUsage DigitalSignature,KeyEncipherment `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")

# Export to PFX
$certPath = "C:\ProgramData\ZLFileRelay\certs"
New-Item -ItemType Directory -Force -Path $certPath

$password = ConvertTo-SecureString -String "YourPassword123!" -Force -AsPlainText
$cert | Export-PfxCertificate -FilePath "$certPath\server.pfx" -Password $password

Write-Host "Certificate created: $certPath\server.pfx"
Write-Host "Password: YourPassword123!"
```

### Option 3: Let's Encrypt (If Server Has Internet)

```bash
# Use certbot or win-acme
# Export as .pfx
# Configure in appsettings.json
```

---

## 🔧 Port Configuration Examples

### Standard Web Ports (Requires Admin)
```json
{
  "Kestrel": {
    "HttpPort": 80,
    "HttpsPort": 443,
    "EnableHttps": true
  }
}
```

**Note:** Ports < 1024 require administrator privileges for the service account.

### Custom High Ports (Recommended for DMZ)
```json
{
  "Kestrel": {
    "HttpPort": 8080,
    "HttpsPort": 8443,
    "EnableHttps": true
  }
}
```

**Advantage:** Can run as LocalSystem without special privileges.

### HTTP Only (Not Recommended for Production)
```json
{
  "Kestrel": {
    "HttpPort": 8080,
    "EnableHttps": false
  }
}
```

**Use Case:** Development, internal networks, behind reverse proxy.

---

## 🛡️ Security Considerations

### Windows Firewall

Installer creates rule automatically for port 8080:
```powershell
netsh advfirewall firewall add rule name="ZL File Relay Web Portal" dir=in action=allow protocol=TCP localport=8080
```

**If you change ports, update firewall:**
```powershell
# Remove old rule
netsh advfirewall firewall delete rule name="ZL File Relay Web Portal"

# Add new rule for custom port
netsh advfirewall firewall add rule name="ZL File Relay Web Portal" dir=in action=allow protocol=TCP localport=YOUR_PORT
```

### Certificate Security

**Best Practices:**
- Use certificates from trusted CA (internal or public)
- Store certificates securely
- Use strong passwords
- Rotate certificates before expiration
- Restrict file permissions on .pfx files

```powershell
# Secure the certificate file
$certFile = "C:\ProgramData\ZLFileRelay\certs\server.pfx"
$acl = Get-Acl $certFile
$acl.SetAccessRuleProtection($true, $false) # Disable inheritance
$acl.Access | ForEach-Object { $acl.RemoveAccessRule($_) } # Remove all
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "LocalSystem", "Read", "Allow")
$acl.AddAccessRule($rule)
Set-Acl $certFile $acl
```

---

## 🔄 Changing Configuration

### After Changing Ports or Certificate

**You MUST restart the Web Portal service:**

```powershell
Restart-Service ZLFileRelay.WebPortal
```

**Or via Config Tool:**
1. Stop Web Portal Service
2. Start Web Portal Service

**Or via Services.msc:**
1. Find "ZL File Relay - Web Portal"
2. Right-click → Restart

---

## 🧪 Testing Configuration

### Test HTTP Access
```powershell
# Local
Invoke-WebRequest -Uri "http://localhost:8080" -UseBasicParsing

# Remote
Invoke-WebRequest -Uri "http://server-name:8080" -UseBasicParsing
```

### Test HTTPS Access
```powershell
# Local (may need -SkipCertificateCheck for self-signed)
Invoke-WebRequest -Uri "https://localhost:8443" -UseBasicParsing -SkipCertificateCheck

# Remote
Invoke-WebRequest -Uri "https://server-name:8443" -UseBasicParsing
```

### Check Service is Listening
```powershell
# Check port is open
netstat -ano | findstr ":8080"
Test-NetConnection localhost -Port 8080

# Check service logs
Get-Content C:\FileRelay\logs\zlfilerelay-web-*.log -Tail 20
```

---

## 📋 Configuration Tool UI (Coming)

### Web Portal Settings Tab

**Server Settings Section:**
```
┌────────────────────────────────────────┐
│ HTTP Port:         [8080          ]    │
│ HTTPS Port:        [8443          ]    │
│ Enable HTTPS:      ☑                   │
├────────────────────────────────────────┤
│ SSL Certificate                        │
│ Path:              [C:\...\cert.pfx]   │
│                    [Browse...]         │
│ Password:          [**************]    │
│                    [Show] [Test]       │
│                                        │
│ Status: ✅ Valid (Expires: 2026-10-08)│
└────────────────────────────────────────┘

[Save Configuration]  [Restart Service]
```

**Features:**
- Port number validation (1-65535)
- Duplicate port detection
- Certificate file browser
- Password show/hide toggle
- Certificate validation
- Expiration date display
- One-click service restart

---

## 🔍 Troubleshooting

### Port Already in Use
```
Error: Address already in use: 0.0.0.0:8080
```

**Solution:** Change port or stop conflicting service:
```powershell
# Find what's using the port
netstat -ano | findstr ":8080"

# Change port in appsettings.json
# Restart service
```

### Certificate Error
```
Error: The certificate chain was issued by an authority that is not trusted
```

**Solution:** 
- Use certificate from trusted CA
- OR add self-signed cert to Trusted Root on clients
- OR use HTTP only (not recommended for production)

### Access Denied
```
HTTP 401 Unauthorized
```

**Solution:**
- Check Windows Authentication is enabled
- Verify user is in allowed groups
- Check service account has network authentication enabled

---

## 📊 Port Recommendations

| Environment | HTTP Port | HTTPS Port | Notes |
|-------------|-----------|------------|-------|
| Development | 5141 | 7089 | Default ASP.NET Core |
| Internal | 8080 | 8443 | No admin required |
| Production | 80 | 443 | Standard, requires admin |
| DMZ | 8080 | 8443 | High ports safer |
| Behind Proxy | 5000 | 5001 | Proxy handles SSL |

---

## 🎯 Recommended Configuration

### For DMZ/OT Environments

```json
{
  "WebPortal": {
    "RequireAuthentication": true,
    "Kestrel": {
      "HttpPort": 8080,
      "HttpsPort": 8443,
      "EnableHttps": true,
      "CertificatePath": "C:\\ProgramData\\ZLFileRelay\\certs\\server.pfx",
      "UseWindowsAuth": true
    }
  }
}
```

**Why:**
- HTTPS for security
- Windows Authentication for access control
- High ports (no admin needed for service account)
- Self-signed cert from internal CA

---

**All configuration will be manageable via the Config Tool GUI!** 🎉


