# Air-Gapped Network Support

## Overview

ZL File Relay is designed to work in **air-gapped (isolated) networks** without internet connectivity, cloud dependencies, or domain trust relationships. This makes it ideal for secure OT/SCADA deployments, classified environments, and isolated DMZ networks.

## Key Capabilities

### ✅ Works Completely Offline

When configured with **Local Authentication**, ZL File Relay requires **zero external dependencies**:

- ❌ **No internet** required
- ❌ **No cloud services** required  
- ❌ **No Active Directory** required
- ❌ **No domain trust** required
- ❌ **No Azure/Microsoft 365** required
- ❌ **No external database** required

### ✅ What DOES Work

- ✅ **Local user accounts** - SQLite database for authentication
- ✅ **File transfer service** - SSH/SCP and SMB transfers work offline
- ✅ **Web portal** - Full upload and management functionality
- ✅ **Self-contained deployment** - All runtime bundled
- ✅ **Windows Service** - Reliable background execution
- ✅ **Comprehensive logging** - All operations logged locally

## Configuration for Air-Gapped Networks

### Recommended Setup

Use **Local Authentication** for air-gapped environments:

```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "Authentication": {
        "EnableEntraId": false,
        "EnableLocalAccounts": true,
        "ConnectionString": "Data Source=C:\\ProgramData\\ZLFileRelay\\zlfilerelay.db",
        "RequireApproval": true
      },
      "AdminEmails": [
        "admin@local.domain",
        "operator@local.domain"
      ],
      "AdminRoles": ["Admin"],
      "UploaderRoles": ["Uploader", "Admin"]
    }
  }
}
```

### Installation

Air-gapped installation works identically to connected deployments:

1. **Copy installer** to target system (via USB, network share, etc.)
2. **Run installer** (self-contained, includes .NET 8)
3. **Configure** via ConfigTool (no internet required)
4. **Create local accounts** via web portal registration
5. **Deploy SSH keys** to SCADA destination servers
6. **Start services** - Everything works offline

### Database

Local authentication uses SQLite database stored locally:

- **Location**: `C:\ProgramData\ZLFileRelay\zlfilerelay.db`
- **Size**: Minimal footprint (< 1MB typically)
- **Backup**: Simple file copy
- **Portable**: Can be moved between servers
- **No network** required

## Hybrid Approach (Optional)

For networks with **intermittent connectivity** or **mixed environments**:

Enable both authentication methods:

```json
{
  "ZLFileRelay": {
    "WebPortal": {
      "Authentication": {
        "EnableEntraId": true,
        "EnableLocalAccounts": true,
        "EntraIdTenantId": "your-tenant-id",
        "EntraIdClientId": "your-client-id",
        "EntraIdClientSecret": "your-secret"
      }
    }
  }
}
```

**Benefits**:
- Internal users → SSO with Entra ID (when internet available)
- External contractors → Local accounts (always available)
- Graceful degradation → Local auth works if cloud unavailable

## Use Cases

### 1. OT/SCADA Networks

Industrial control systems often isolated from IT networks:

- ✅ **No domain trust** between IT and OT
- ✅ **No internet** in OT environment
- ✅ **Local operator accounts** only
- ✅ **Full functionality** offline

### 2. Classified/Secure Environments

Government and defense installations:

- ✅ **Air-gapped** network requirements
- ✅ **No cloud** dependencies
- ✅ **Local audit trails** 
- ✅ **Secure credential storage** (DPAPI)

### 3. Isolated DMZ Networks

Perimeter networks with minimal connectivity:

- ✅ **No AD trust** to internal domains
- ✅ **Self-contained** authentication
- ✅ **Independent** of corporate identity
- ✅ **Bridge** between corp and SCADA networks

### 4. Legacy Systems

Systems without modern identity infrastructure:

- ✅ **No Azure AD** available
- ✅ **No modern** SSO solutions
- ✅ **Simple** local account management
- ✅ **Migration path** if identity modernized later

## Comparison: Local vs Entra ID

| Feature | Local Authentication | Entra ID (Azure AD) |
|---------|---------------------|---------------------|
| **Internet Required** | ❌ No | ✅ Yes |
| **Cloud Dependencies** | ❌ No | ✅ Yes |
| **Domain Trust** | ❌ No | ❌ No |
| **Air-Gapped Support** | ✅ Yes | ❌ No |
| **User Management** | Manual (database/SQL) | Azure Portal |
| **Password Reset** | Manual/Admin | Self-service |
| **MFA Support** | ❌ No | ✅ Yes |
| **Conditional Access** | ❌ No | ✅ Yes |
| **SSO Integration** | ❌ No | ✅ Yes |
| **Setup Complexity** | Low | Medium |
| **Offline Operation** | ✅ Yes | ❌ No |

## User Management in Air-Gapped Environments

### Creating Users

**Method 1: Self-Registration** (Recommended)
1. Enable `EnableLocalAccounts: true`
2. Users visit web portal and click "Register"
3. Provide email and password
4. Admin approves (if `RequireApproval: true`)

**Method 2: Admin Creation** (Manual)
1. Use database management tool (e.g., DBeaver)
2. Open `zlfilerelay.db`
3. Insert user records into `AspNetUsers` table
4. Assign roles in `AspNetUserRoles` table

### Admin Operations

**Via SQLite**:
```sql
-- List users
SELECT Id, UserName, Email, IsApproved FROM AspNetUsers;

-- Approve user
UPDATE AspNetUsers 
SET IsApproved = 1, ApprovedDate = datetime('now') 
WHERE Email = 'user@example.com';

-- Grant Admin role
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id 
FROM AspNetUsers u, AspNetRoles r
WHERE u.Email = 'admin@example.com' AND r.Name = 'Admin';
```

## Security Considerations

### ✅ Security Features Available

- **Password encryption** - ASP.NET Core Identity hashing
- **Session cookies** - HTTPOnly, Secure flags
- **Rate limiting** - Prevents brute force
- **Account lockout** - After failed attempts
- **Audit logging** - All operations logged
- **DPAPI encryption** - For service credentials

### ⚠️ Limitations

- **No automatic MFA** - Must use strong passwords
- **No password reset email** - Manual admin reset only
- **No modern SSO** - Requires separate logins
- **No conditional access** - No risk-based auth

### 🔒 Best Practices

1. **Strong passwords** - 8+ characters, complexity required
2. **Regular rotation** - Periodically change user passwords
3. **Minimal accounts** - Create only necessary users
4. **Approval workflow** - Enable `RequireApproval`
5. **Regular backup** - Backup `zlfilerelay.db` database
6. **Monitoring** - Review logs for suspicious activity

## Deployment Checklist

### Pre-Deployment

- [ ] Installer copied to target system
- [ ] .NET 8 bundled (self-contained deployment)
- [ ] Network ports configured (8080/8443)
- [ ] Firewall rules configured
- [ ] SSH keys generated (if using SSH transfer)
- [ ] SCADA destination configured

### Configuration

- [ ] Local authentication enabled
- [ ] Entra ID disabled
- [ ] Admin email(s) configured
- [ ] Roles configured (Admin/Uploader)
- [ ] Database path configured
- [ ] Approval workflow configured

### Post-Deployment

- [ ] Service started and running
- [ ] Database created successfully
- [ ] Initial admin account created
- [ ] Test login successful
- [ ] File upload tested
- [ ] Transfer to SCADA tested

## Monitoring

### Health Checks

Monitor these indicators in air-gapped environments:

```powershell
# Service status
Get-Service ZLFileRelay*

# Database file
Test-Path C:\ProgramData\ZLFileRelay\zlfilerelay.db

# Recent logs
Get-Content C:\FileRelay\logs\zlfilerelay-*.log -Tail 50

# User count
sqlite3 C:\ProgramData\ZLFileRelay\zlfilerelay.db "SELECT COUNT(*) FROM AspNetUsers"
```

### Logs to Review

- **Application logs**: User sign-ins, uploads, transfers
- **Security logs**: Failed login attempts, authorization failures
- **System logs**: Service start/stop, errors
- **Database logs**: If SQLite logging enabled

## Troubleshooting

### Users Can't Log In

**Symptoms**: Login page returns to login

**Solutions**:
1. Check database file exists and is accessible
2. Verify user account exists: `SELECT * FROM AspNetUsers WHERE Email = 'user@example.com'`
3. Check if user is approved: `SELECT IsApproved FROM AspNetUsers WHERE Email = 'user@example.com'`
4. Verify user has appropriate role
5. Review application logs for errors

### Registration Not Working

**Symptoms**: Register button missing or fails

**Solutions**:
1. Verify `EnableLocalAccounts: true` in config
2. Check database permissions
3. Verify password meets requirements
4. Check for duplicate email addresses
5. Review logs for database errors

### Transfer Fails

**Symptoms**: Files don't transfer to SCADA

**Solutions**:
- Transfer service independent of authentication
- Check SSH/SMB connectivity from service account
- Verify credentials in `C:\ProgramData\ZLFileRelay\credentials`
- Review transfer service logs

## Backup & Recovery

### Backup Strategy

**Critical Files**:
```powershell
# Database
Copy-Item C:\ProgramData\ZLFileRelay\zlfilerelay.db C:\Backups\

# Configuration
Copy-Item C:\ProgramData\ZLFileRelay\appsettings.json C:\Backups\

# SSH keys (if applicable)
Copy-Item C:\ProgramData\ZLFileRelay\ssh\* C:\Backups\ssh\ -Recurse

# Credentials (encrypted)
Copy-Item C:\ProgramData\ZLFileRelay\credentials C:\Backups\
```

### Recovery Process

1. **Restore database** from backup
2. **Restore configuration** from backup
3. **Restore SSH keys** if applicable
4. **Restore credentials** if applicable
5. **Restart services**
6. **Verify functionality**

## Conclusion

ZL File Relay's air-gapped support makes it an excellent choice for secure, isolated network deployments where cloud dependencies are not possible or permitted. The local authentication option provides full functionality while maintaining security through industry-standard practices.

**Choose Local Authentication when:**
- Deploying to air-gapped networks
- No internet connectivity available
- Security policies prohibit cloud services
- OT/SCADA network isolation required
- Simple, self-contained solution needed

**Choose Hybrid (Local + Entra ID) when:**
- Network has intermittent connectivity
- Mix of internal and external users
- Want SSO when available but offline fallback
- Transitioning from air-gapped to connected

**Choose Entra ID only when:**
- Always connected to internet
- Modern identity infrastructure available
- MFA and conditional access required
- Centralized user management desired

## Related Documentation

- [Hybrid Authentication Implementation](HYBRID_AUTHENTICATION_IMPLEMENTATION.md)
- [User Management Guide](deployment/USER_MANAGEMENT.md)
- [Configuration Reference](configuration/CONFIGURATION.md)
- [DMZ Deployment Guide](deployment/DMZ_DEPLOYMENT.md)

