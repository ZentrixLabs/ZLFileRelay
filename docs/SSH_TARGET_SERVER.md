# SSH Target Server Setup

Guide for configuring SSH on the target server to receive file transfers from ZL File Relay.

---

## Overview

ZL File Relay uses SSH/SCP (Secure Copy Protocol) to transfer files. This guide covers setting up the target server (usually a Linux/Unix system) to accept SSH connections from the ZL File Relay service.

---

## Prerequisites

- Root or sudo access on target server
- SSH server installed (usually pre-installed on Linux)
- Network connectivity from ZL File Relay server to target server on port 22

---

## Quick Setup

### 1. Create Service Account

Create a dedicated user for file transfers:

```bash
# Create user (no login shell for security)
sudo useradd -m -s /bin/bash svc_filetransfer

# Create incoming directory
sudo mkdir -p /data/incoming
sudo chown svc_filetransfer:svc_filetransfer /data/incoming
sudo chmod 755 /data/incoming
```

### 2. Configure SSH Keys

On the **ZL File Relay server**, generate SSH keys using the Configuration Tool:

1. Open Configuration Tool
2. Go to **File Transfer** tab → **SSH Settings**
3. Click **Generate SSH Key Pair**
4. Click **Copy Public Key to Clipboard**

On the **target server**, add the public key:

```bash
# Switch to service account
sudo su - svc_filetransfer

# Create .ssh directory
mkdir -p ~/.ssh
chmod 700 ~/.ssh

# Add public key (paste from clipboard)
nano ~/.ssh/authorized_keys
# Paste the public key, then save (Ctrl+O, Enter, Ctrl+X)

# Set correct permissions
chmod 600 ~/.ssh/authorized_keys

# Exit back to your user
exit
```

### 3. Test Connection

From the **ZL File Relay server**, test the SSH connection:

```powershell
# Using PowerShell
ssh -i C:\ProgramData\ZLFileRelay\.ssh\id_rsa svc_filetransfer@target-server
```

If successful, you'll get a shell prompt on the target server.

---

## Detailed Configuration

### SSH Server Configuration

Edit SSH server configuration on target server:

```bash
sudo nano /etc/ssh/sshd_config
```

**Recommended settings**:

```
# Allow public key authentication
PubkeyAuthentication yes

# Location of authorized_keys
AuthorizedKeysFile .ssh/authorized_keys

# Disable password authentication (more secure)
PasswordAuthentication no

# Disable root login
PermitRootLogin no

# Limit to specific user (optional)
AllowUsers svc_filetransfer

# Enable SFTP subsystem (required for SCP)
Subsystem sftp /usr/lib/openssh/sftp-server
```

**Restart SSH service**:

```bash
# Ubuntu/Debian
sudo systemctl restart sshd

# RHEL/CentOS
sudo systemctl restart sshd

# Verify SSH is running
sudo systemctl status sshd
```

---

## Security Hardening

### Restrict User Access

Limit the service account to only file transfer operations:

```bash
# Edit sudoers (if needed for specific operations)
sudo visudo

# Add line to restrict commands
svc_filetransfer ALL=(ALL) NOPASSWD: /bin/mv, /bin/cp

# Or better: Use a restricted shell
sudo usermod -s /usr/bin/rssh svc_filetransfer
```

### Firewall Configuration

Allow SSH only from ZL File Relay server:

```bash
# Ubuntu/Debian (ufw)
sudo ufw allow from 192.168.1.50 to any port 22
sudo ufw enable

# RHEL/CentOS (firewalld)
sudo firewall-cmd --permanent --add-rich-rule='rule family="ipv4" source address="192.168.1.50" port protocol="tcp" port="22" accept'
sudo firewall-cmd --reload
```

Replace `192.168.1.50` with your ZL File Relay server IP.

### Directory Permissions

Set up proper directory structure with correct permissions:

```bash
# Create directory structure
sudo mkdir -p /data/incoming
sudo mkdir -p /data/processed
sudo mkdir -p /data/failed

# Set ownership
sudo chown -R svc_filetransfer:svc_filetransfer /data

# Set permissions (owner full, group read, others none)
sudo chmod 750 /data/incoming
sudo chmod 750 /data/processed
sudo chmod 750 /data/failed
```

### SELinux Configuration (RHEL/CentOS)

If SELinux is enabled:

```bash
# Check SELinux status
sestatus

# If enforcing, configure SSH contexts
sudo semanage fcontext -a -t ssh_home_t "/home/svc_filetransfer/.ssh(/.*)?"
sudo restorecon -R -v /home/svc_filetransfer/.ssh

# Allow SSH to write to /data
sudo semanage fcontext -a -t public_content_rw_t "/data(/.*)?"
sudo restorecon -R -v /data
```

---

## Troubleshooting

### Connection Refused

**Problem**: `Connection refused` or `No route to host`

**Solutions**:

1. **Check SSH service is running**:
   ```bash
   sudo systemctl status sshd
   ```

2. **Check firewall**:
   ```bash
   # Ubuntu
   sudo ufw status
   
   # RHEL/CentOS
   sudo firewall-cmd --list-all
   ```

3. **Verify port 22 is listening**:
   ```bash
   sudo netstat -tlnp | grep :22
   ```

4. **Test from ZL File Relay server**:
   ```powershell
   Test-NetConnection -ComputerName target-server -Port 22
   ```

---

### Permission Denied (publickey)

**Problem**: `Permission denied (publickey,gssapi-keyex,gssapi-with-mic)`

**Solutions**:

1. **Check authorized_keys permissions**:
   ```bash
   ls -la ~/.ssh/authorized_keys
   # Should be: -rw------- (600)
   ```

2. **Check .ssh directory permissions**:
   ```bash
   ls -la ~/.ssh
   # Should be: drwx------ (700)
   ```

3. **Check home directory permissions**:
   ```bash
   ls -la ~ | grep "^d"
   # Should be: drwxr-xr-x or stricter
   ```

4. **Verify public key format**:
   ```bash
   # Should start with: ssh-rsa AAAAB3...
   head -c 50 ~/.ssh/authorized_keys
   ```

5. **Check SSH logs on target**:
   ```bash
   sudo tail -f /var/log/auth.log     # Ubuntu/Debian
   sudo tail -f /var/log/secure       # RHEL/CentOS
   ```

---

### SCP Transfer Fails

**Problem**: Connection succeeds but file transfer fails

**Solutions**:

1. **Check destination directory exists**:
   ```bash
   ls -la /data/incoming
   ```

2. **Check write permissions**:
   ```bash
   sudo su - svc_filetransfer
   touch /data/incoming/test.txt
   rm /data/incoming/test.txt
   ```

3. **Check disk space**:
   ```bash
   df -h /data
   ```

4. **Check SFTP subsystem**:
   ```bash
   grep -i "subsystem.*sftp" /etc/ssh/sshd_config
   # Should show: Subsystem sftp /usr/lib/openssh/sftp-server
   ```

---

### Slow Transfer Performance

**Problem**: Transfers are very slow

**Solutions**:

1. **Check network speed**:
   ```bash
   # Install iperf3 on both servers
   # On target server:
   iperf3 -s
   
   # On ZL File Relay server:
   iperf3 -c target-server
   ```

2. **Enable SSH compression** (in ZL File Relay SSH settings):
   - Add compression settings in Configuration Tool (future feature)

3. **Check CPU usage during transfer**:
   ```bash
   top
   # Look for high CPU on sshd or sftp-server
   ```

4. **Check for packet loss**:
   ```bash
   ping -c 100 target-server
   ```

---

## Testing File Transfer

### Manual Test

Test file transfer manually from ZL File Relay server:

```powershell
# Create test file
echo "Test content" > C:\temp\test.txt

# Transfer using SCP
scp -i C:\ProgramData\ZLFileRelay\.ssh\id_rsa C:\temp\test.txt svc_filetransfer@target-server:/data/incoming/

# Verify on target server
ssh -i C:\ProgramData\ZLFileRelay\.ssh\id_rsa svc_filetransfer@target-server "ls -la /data/incoming/"
```

### Automated Test from Configuration Tool

1. Open Configuration Tool
2. Go to **File Transfer** tab → **SSH Settings**
3. Click **Test Connection**
4. Should show "Connection successful"

---

## Advanced Configuration

### SSH Key Rotation

Rotate SSH keys periodically for security:

```bash
# On target server
# 1. Backup current keys
sudo cp /home/svc_filetransfer/.ssh/authorized_keys /home/svc_filetransfer/.ssh/authorized_keys.backup

# 2. Generate new keys in ZL File Relay Configuration Tool

# 3. Add new public key to authorized_keys (don't delete old one yet)
sudo nano /home/svc_filetransfer/.ssh/authorized_keys
# Add new key on a new line

# 4. Test with new key

# 5. Remove old key from authorized_keys
sudo nano /home/svc_filetransfer/.ssh/authorized_keys
# Delete old key line
```

### Multiple Source Servers

To accept connections from multiple ZL File Relay servers:

```bash
# Add multiple public keys to authorized_keys (one per line)
sudo nano /home/svc_filetransfer/.ssh/authorized_keys

# Example:
ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQC... server1@zlfilerelay
ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQD... server2@zlfilerelay
ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABgQE... server3@zlfilerelay
```

### Restrict Command Execution

Force specific command execution only (advanced):

```bash
# Edit authorized_keys with command restrictions
sudo nano /home/svc_filetransfer/.ssh/authorized_keys

# Add command restriction before key:
command="/usr/bin/rssh" ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAAB...
```

Install and configure rssh (restricted shell):

```bash
# Install rssh
sudo apt-get install rssh         # Ubuntu/Debian
sudo yum install rssh              # RHEL/CentOS

# Configure rssh
sudo nano /etc/rssh.conf
# Uncomment: allowscp
# Uncomment: allowsftp

# Set user's shell to rssh
sudo usermod -s /usr/bin/rssh svc_filetransfer
```

---

## Windows Target Server (Optional)

If target server is Windows (less common):

### Install OpenSSH Server

```powershell
# Windows Server 2019+
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0

# Start and enable service
Start-Service sshd
Set-Service -Name sshd -StartupType 'Automatic'

# Configure firewall
New-NetFirewallRule -Name sshd -DisplayName 'OpenSSH Server (sshd)' -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
```

### Add Public Key

```powershell
# Create .ssh directory in user profile
$userProfile = "C:\Users\svc_filetransfer"
New-Item -ItemType Directory -Path "$userProfile\.ssh" -Force

# Add public key
$publicKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAAB..."
Add-Content -Path "$userProfile\.ssh\authorized_keys" -Value $publicKey

# Set permissions (administrators and system only)
icacls "$userProfile\.ssh\authorized_keys" /inheritance:r
icacls "$userProfile\.ssh\authorized_keys" /grant "NT AUTHORITY\SYSTEM:(R)"
icacls "$userProfile\.ssh\authorized_keys" /grant "BUILTIN\Administrators:(R)"
icacls "$userProfile\.ssh\authorized_keys" /grant "svc_filetransfer:(R)"
```

---

## Monitoring and Logging

### Enable SSH Logging

Increase SSH logging verbosity for debugging:

```bash
sudo nano /etc/ssh/sshd_config

# Set log level
LogLevel VERBOSE

# Restart SSH
sudo systemctl restart sshd
```

### Monitor Failed Login Attempts

```bash
# View failed login attempts
sudo grep "Failed password" /var/log/auth.log | tail -20

# View all SSH authentication attempts
sudo grep "sshd" /var/log/auth.log | tail -50
```

### Set Up Log Monitoring (Optional)

Use `fail2ban` to automatically block repeated failed attempts:

```bash
# Install fail2ban
sudo apt-get install fail2ban      # Ubuntu/Debian
sudo yum install fail2ban           # RHEL/CentOS

# Enable and start
sudo systemctl enable fail2ban
sudo systemctl start fail2ban

# Check status
sudo fail2ban-client status sshd
```

---

## Checklist

Before completing setup, verify:

- [ ] SSH service is running on target server
- [ ] Service account created with proper permissions
- [ ] Public key added to `~/.ssh/authorized_keys`
- [ ] File permissions correct (700 for .ssh, 600 for authorized_keys)
- [ ] Destination directory exists and is writable
- [ ] Firewall allows SSH from ZL File Relay server
- [ ] Test connection succeeds from Configuration Tool
- [ ] Manual SCP transfer test succeeds
- [ ] SSH logs show successful authentication
- [ ] Disk space available for incoming files

---

## Getting Help

If you encounter issues:

1. **Check target server SSH logs**:
   - Ubuntu/Debian: `/var/log/auth.log`
   - RHEL/CentOS: `/var/log/secure`

2. **Check ZL File Relay logs**:
   - `C:\FileRelay\logs\zlfilerelay-service-[date].log`

3. **Test SSH manually**:
   ```powershell
   ssh -vvv -i C:\ProgramData\ZLFileRelay\.ssh\id_rsa svc_filetransfer@target-server
   ```
   (The `-vvv` flag enables verbose output showing exactly what fails)

4. **Refer to main setup guide**: [SETUP.md](SETUP.md)

---

**ZL File Relay** - Secure SSH File Transfer

