# Improved Configuration Save Error Handling

## Overview

Enhanced the configuration save functionality to provide detailed error messages and better debugging information when saves fail.

## Problem Addressed

Previously, when configuration saves failed, users only saw a generic "Failed to save configuration" message with no indication of what went wrong or how to fix it.

## Improvements Made

### 1. Enhanced WebPortalViewModel.SaveConfigurationAsync()

**Better Error Reporting:**
- Added specific exception handling for different error types
- Shows the exact configuration file path being saved to
- Provides actionable guidance for each error type

**Pre-Save Validation:**
- Validates configuration before attempting to save
- Checks if logo file exists if path is provided
- Prevents saves with invalid data

**Detailed Error Messages:**
- **Permission Issues**: Clear guidance to run as Administrator
- **File Access Issues**: Explains possible causes (locked files, disk full)
- **Validation Errors**: Shows specific validation failures
- **Directory Issues**: Explains missing directories

### 2. Enhanced ConfigurationService.SaveAsync()

**Comprehensive Logging:**
- Logs each step of the save process
- Records file sizes and paths
- Provides detailed error context

**Better Error Handling:**
- Throws specific exceptions instead of swallowing errors
- Creates configuration directories if they don't exist
- Validates configuration before saving

**Enhanced Validation:**
- Added Web Portal specific validation
- Added Branding settings validation
- Validates file existence for certificates and logos

### 3. New Validation Rules

**Web Portal Validation:**
- HTTP/HTTPS port range validation (1-65535)
- Port uniqueness validation
- Certificate file existence when HTTPS enabled

**Branding Validation:**
- Required company name and site name
- Logo file existence validation

## Error Message Examples

### Before
```
❌ Failed to save configuration
```

### After
```
❌ Permission denied saving to:
C:\ProgramData\ZLFileRelay\appsettings.json

Error: Access to the path is denied.

Try running the configuration tool as Administrator or check file permissions.
```

## Configuration File Path Display

The application now shows users exactly where the configuration is being saved:

```
✅ Configuration saved successfully to:
C:\ProgramData\ZLFileRelay\appsettings.json

Restart Web Portal service to apply changes.
```

## Debugging Information

When saves fail, users now get:

1. **Exact file path** being saved to
2. **Specific error message** from the system
3. **Actionable guidance** on how to fix the issue
4. **Logging information** for administrators

## Common Error Scenarios Handled

1. **Permission Denied**
   - Cause: Insufficient file permissions
   - Solution: Run as Administrator

2. **File Locked**
   - Cause: Another process has the file open
   - Solution: Close other applications, restart services

3. **Directory Not Found**
   - Cause: Configuration directory moved/deleted
   - Solution: Application will create directory automatically

4. **Validation Failures**
   - Cause: Invalid configuration data
   - Solution: Fix the specific validation errors shown

5. **Logo/Certificate File Not Found**
   - Cause: Referenced file doesn't exist
   - Solution: Check file paths or browse for correct files

## Benefits

1. **Faster Troubleshooting**: Users know exactly what went wrong
2. **Better User Experience**: Clear guidance on how to fix issues
3. **Reduced Support Load**: Self-service problem resolution
4. **Better Debugging**: Detailed logging for administrators
5. **Proactive Validation**: Catches issues before save attempts

## Technical Implementation

- Enhanced exception handling with specific catch blocks
- Added configuration file path display
- Improved validation with detailed error messages
- Comprehensive logging throughout the save process
- Automatic directory creation for configuration files

This improvement significantly enhances the user experience when configuration saves fail and provides the information needed to resolve issues quickly.
