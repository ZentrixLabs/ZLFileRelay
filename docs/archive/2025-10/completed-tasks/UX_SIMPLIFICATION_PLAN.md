# ZL File Relay Config Tool - UX Simplification Plan

## Executive Summary

The Config Tool UI has grown to 9 tabs with significant overlap and fragmentation. This plan consolidates to **6 intuitive tabs** with better organization and reduced cognitive load.

## Current State Analysis

### Current 9 Tabs
1. **Dashboard** - Overview (UserControl)
2. **Service Management** - Start/stop service, credentials status (inline)
3. **Configuration Settings** - Directories, file handling (inline)
4. **Web Portal** - Web server config (inline)
5. **SSH / Transfer** - SSH settings (inline)
6. **Service Account** - Account management, permissions (inline)
7. **Security** - File policies, upload limits (inline)
8. **Advanced** - Remote management (UserControl)
9. **About** - Version info (UserControl)

### Problems Identified
- ❌ Too many tabs (9) - users get lost
- ❌ Fragmented configuration (spread across tabs 3, 5, 7)
- ❌ Service-related settings split (tabs 2 & 6)
- ❌ MainWindow.xaml is 1,207 lines (too large)
- ❌ Inconsistent implementation (some UserControls, some inline)
- ❌ User confusion: "Where do I configure X?"

## Proposed Structure (6 Tabs)

### 1. 🏠 Dashboard
**Purpose:** System overview and health at a glance  
**Content:**
- System health status
- Service status
- Transfer statistics
- Recent activity
- Quick action buttons

**Implementation:** Keep existing DashboardView.xaml ✅

---

### 2. ⚙️ Service
**Purpose:** Everything related to the Windows service  
**Content:**
- **Service Controls**
  - Install/Uninstall
  - Start/Stop/Restart
  - Status indicator
- **Service Account**
  - Current account info
  - Change account
  - Grant logon rights
  - Fix folder permissions
- **Activity Log**
  - Real-time service operations
  - Clear log button

**Implementation:** 
- Create new `ServiceView.xaml` UserControl
- Merge content from "Service Management" + "Service Account" tabs
- Create `ServiceViewModel.cs` to unify the logic

---

### 3. 📁 File Transfer
**Purpose:** Configure what files go where and how  
**Content:**
- **Transfer Method Selection**
  - Radio buttons: SSH/SCP or SMB/CIFS
- **Directories**
  - Watch directory
  - Archive directory
  - Browse buttons
- **SSH Configuration** (when SSH selected)
  - Host, port, username
  - Key management (generate, view, copy)
  - Connection test
  - Destination path
- **SMB Configuration** (when SMB selected)
  - Server, share path
  - Credentials button → opens secure dialog
- **File Handling**
  - Archive after transfer
  - Delete after transfer (warning)
  - Verify integrity
- **Security Policies** (moved from Security tab)
  - Allow executables (with warning)
  - Allow hidden files
  - File size limits (slider with presets)
- **Save Configuration** button

**Implementation:**
- Create new `FileTransferView.xaml` UserControl
- Merge: "Configuration Settings" + "SSH / Transfer" + relevant "Security" items
- Create `FileTransferViewModel.cs`
- Use Visibility binding to show/hide SSH vs SMB sections

**Benefits:**
- All transfer-related config in one place
- Contextual (SSH/SMB sections show based on selection)
- Security policies right where they're needed

---

### 4. 🌐 Web Portal
**Purpose:** Configure the web upload interface  
**Content:**
- **Server Configuration**
  - HTTP/HTTPS ports
  - Enable HTTPS checkbox
- **SSL Certificate**
  - Certificate path (with browse)
  - Password
  - Test certificate button
  - Status indicator
- **Authentication** (add from config)
  - Enable Windows Authentication
  - Allowed groups
- **Upload Settings**
  - Upload directory (with browse)
  - Max file size
- **Save & Restart** buttons

**Implementation:**
- Create new `WebPortalView.xaml` UserControl
- Extract from current inline "Web Portal" tab
- Create `WebPortalViewModel.cs`
- Add missing auth settings from appsettings.json

---

### 5. 🔧 Advanced
**Purpose:** Advanced features and remote management  
**Content:**
- **Remote Server Connection**
  - Local vs Remote mode
  - Server connection settings
  - Credential management
  - Test connection
- **Audit & Compliance**
  - Enable audit logging
  - Audit log path
  - Retention settings
- **Advanced Diagnostics**
  - View full config file
  - Export diagnostics
  - Reset to defaults (with confirmation)

**Implementation:**
- Enhance existing `RemoteServerView.xaml`
- Add audit logging section (from Security tab)
- Add diagnostics tools
- Keep `RemoteServerViewModel.cs`, add diagnostic methods

---

### 6. ℹ️ About
**Purpose:** Version info, help, and support  
**Content:**
- Product name and version
- Copyright and license
- System information
- Links to documentation
- Support contact

**Implementation:** Keep existing AboutView.xaml ✅

---

## Migration Plan

### Phase 1: Create New UserControls (30 min)
1. Create `ServiceView.xaml` + `ServiceViewModel.cs`
2. Create `FileTransferView.xaml` + `FileTransferViewModel.cs`
3. Create `WebPortalView.xaml` + `WebPortalViewModel.cs`

### Phase 2: Extract & Migrate Content (45 min)
1. Move Service Management + Service Account → ServiceView
2. Move Configuration + SSH + Security (file policies) → FileTransferView
3. Move Web Portal inline → WebPortalView
4. Update RemoteServerView with audit logging

### Phase 3: Update MainWindow (15 min)
1. Simplify tab structure to 6 tabs
2. Replace inline content with `<ContentControl>` hosting views
3. Wire up view models in code-behind
4. Update status bar

### Phase 4: Testing & Polish (30 min)
1. Test all functionality in each tab
2. Verify save/load works correctly
3. Check that all features are still accessible
4. Polish contextual help and tooltips

**Total Estimated Time:** 2 hours

---

## Benefits Summary

✅ **Reduced from 9 tabs to 6 tabs** (33% reduction)  
✅ **Logical grouping** - related features together  
✅ **Consistent implementation** - all major tabs are UserControls  
✅ **Better maintainability** - smaller, focused files  
✅ **Improved UX** - users can find what they need faster  
✅ **Reduced MainWindow size** - from 1,207 lines to ~300 lines  
✅ **Contextual configuration** - SSH/SMB sections show when relevant  
✅ **Security in context** - file policies where you configure transfers  

---

## Tab Navigation Reference

**Old → New Mapping:**

| Old Tab | New Location |
|---------|--------------|
| Dashboard | Dashboard (unchanged) |
| Service Management | Service |
| Configuration Settings | File Transfer |
| Web Portal | Web Portal |
| SSH / Transfer | File Transfer |
| Service Account | Service |
| Security | File Transfer (file policies) + Advanced (audit) |
| Advanced | Advanced (unchanged) |
| About | About (unchanged) |

---

## Visual Hierarchy

```
Dashboard          - Quick overview, health at a glance
├─ Service         - Manage the Windows service
├─ File Transfer   - Configure transfers (SSH/SMB + policies)
├─ Web Portal      - Configure web upload interface  
├─ Advanced        - Remote management + audit + diagnostics
└─ About           - Info and help
```

This creates a clear mental model: Dashboard → Service → Transfer → Web → Advanced → About

---

## Implementation Notes

### Contextual UI Patterns

**File Transfer Tab:**
```
[Radio] SSH/SCP  [Radio] SMB/CIFS

[If SSH selected]
  ┌─────────────────────────┐
  │ SSH Configuration       │
  │ - Host, Port, Username  │
  │ - Key Management        │
  │ - Test Connection       │
  └─────────────────────────┘

[If SMB selected]
  ┌─────────────────────────┐
  │ SMB Configuration       │
  │ - Server, Share         │
  │ - Configure Credentials │
  └─────────────────────────┘

[Always visible]
  ┌─────────────────────────┐
  │ Directories             │
  │ File Handling Options   │
  │ Security Policies       │
  └─────────────────────────┘
```

### Help System
- Keep contextual help expanders in each section
- Add tooltips to all important fields
- Consider adding a "?" button that links to specific doc pages

### Consistency
- All UserControls follow same pattern
- Consistent spacing (20px margins)
- Consistent button styles (Primary, Secondary, Destructive)
- Consistent section headers (SubtitleTextBlockStyle)

---

## Future Enhancements (Post-Simplification)

1. **Wizard Mode** - First-time setup wizard for new installations
2. **Templates** - Save/load configuration profiles
3. **Quick Setup** - Preset configs for common scenarios (DMZ, Internal, Dev)
4. **Search** - Search across all settings
5. **Keyboard Shortcuts** - Ctrl+S to save, Ctrl+T to test, etc.

---

## User Testing Questions

After implementation, validate with these questions:
- ✓ Can users find service controls within 5 seconds?
- ✓ Is it clear where to configure SSH vs SMB?
- ✓ Are security settings discoverable?
- ✓ Does the flow feel logical?
- ✓ Are there any missing features?

---

## Approval Checklist

Before proceeding with implementation:
- [ ] Review proposed tab structure
- [ ] Confirm no features are being removed
- [ ] Validate grouping logic makes sense
- [ ] Approve migration timeline
- [ ] Review visual mockups (if needed)

---

**Status:** 📋 Plan Complete - Ready for Implementation  
**Next Step:** Create UserControls and begin migration  
**Timeline:** ~2 hours for complete implementation  

