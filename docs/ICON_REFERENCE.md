# Icon Reference Guide

Complete reference for using Font Awesome icons in ZL File Relay components.

## Overview

ZL File Relay uses **Font Awesome** for all icons across both the Web Portal and Config Tool:

- **Web Portal**: Font Awesome self-hosted CSS/fonts
- **Config Tool (WPF)**: FontAwesome.Sharp NuGet package

This ensures consistent, professional iconography throughout the application.

---

## Web Portal (ASP.NET Core)

### Setup

Font Awesome is self-hosted in `wwwroot/lib/fontawesome/` for air-gapped deployment compatibility.

**In `_Layout.cshtml`:**
```html
<link rel="stylesheet" href="~/lib/fontawesome/css/all.min.css" />
```

### Usage in Razor Pages

```html
<!-- Basic icon -->
<i class="fa-solid fa-cloud-arrow-up"></i>

<!-- Icon with text -->
<button class="btn btn-primary">
    <i class="fa-solid fa-save"></i> Save Configuration
</button>

<!-- Colored icon -->
<i class="fa-solid fa-circle-check text-success"></i>

<!-- Sized icon -->
<i class="fa-solid fa-warning fa-2x"></i>
```

### Icon Style Classes

Font Awesome has multiple style variants:

- `fa-solid` - Solid filled icons (most common)
- `fa-regular` - Outline/hollow icons (Pro only)
- `fa-light` - Thin outline icons (Pro only)
- `fa-duotone` - Two-tone colored icons (Pro only)
- `fa-brands` - Brand/logo icons (free)

**For this project, use `fa-solid` by default.**

### Icon Sizing

```html
<i class="fa-solid fa-home fa-xs"></i>     <!-- Extra small -->
<i class="fa-solid fa-home fa-sm"></i>     <!-- Small -->
<i class="fa-solid fa-home"></i>           <!-- Default (16px) -->
<i class="fa-solid fa-home fa-lg"></i>     <!-- Large (1.33x) -->
<i class="fa-solid fa-home fa-xl"></i>     <!-- Extra large (1.5x) -->
<i class="fa-solid fa-home fa-2x"></i>     <!-- 2x size -->
<i class="fa-solid fa-home fa-3x"></i>     <!-- 3x size -->
```

### Common Web Portal Icons

| Icon Name | Class | Usage |
|-----------|-------|-------|
| Upload | `fa-cloud-arrow-up` | File uploads |
| User | `fa-circle-user` | User profile |
| Network | `fa-network-wired` | Product logo |
| Location | `fa-location-dot` | Site name |
| Success | `fa-circle-check` | Success messages |
| Error | `fa-circle-xmark` | Error messages |
| Warning | `fa-triangle-exclamation` | Warnings |
| Info | `fa-circle-info` | Information |
| Security | `fa-shield-check` | Security features |
| Email | `fa-envelope` | Contact info |
| File | `fa-file-lines` | File listings |
| Folder | `fa-folder` | Directories |
| Home | `fa-house` | Home page |

---

## WPF Config Tool (FontAwesome.Sharp)

### Setup

FontAwesome.Sharp is installed via NuGet:
```xml
<PackageReference Include="FontAwesome.Sharp" Version="6.3.0" />
```

### Using IconResources

The `IconResources.cs` class provides strongly-typed icon references:

```csharp
using ZLFileRelay.ConfigTool.Resources;
using FontAwesome.Sharp;

// Access icons via IconResources
IconChar myIcon = IconResources.Save;
IconChar anotherIcon = IconResources.Refresh;
```

### Usage in XAML

#### Method 1: IconImage Control (Recommended)

```xaml
xmlns:fa="http://schemas.awesome.incremented/wpf/xaml/fontawesome.sharp"

<!-- Simple icon -->
<fa:IconImage Icon="{x:Static fa:IconChar.Save}" 
              Foreground="Blue" 
              Width="20" 
              Height="20"/>

<!-- Icon in button -->
<Button Style="{StaticResource PrimaryButton}">
    <StackPanel Orientation="Horizontal">
        <fa:IconImage Icon="{x:Static fa:IconChar.Save}" 
                      Foreground="White" 
                      Width="16" 
                      Height="16" 
                      Margin="0,0,8,0"/>
        <TextBlock Text="Save Configuration" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

#### Method 2: IconBlock Control

For inline text with icons:

```xaml
<TextBlock>
    <fa:IconBlock Icon="{x:Static fa:IconChar.Check}" Foreground="Green"/>
    <Run Text=" Configuration Valid"/>
</TextBlock>
```

#### Method 3: Using IconResources

```xaml
xmlns:resources="clr-namespace:ZLFileRelay.ConfigTool.Resources"

<fa:IconImage Icon="{x:Static resources:IconResources.Save}" 
              Width="20" 
              Height="20"/>
```

### Icon Properties

```xaml
<fa:IconImage Icon="{x:Static fa:IconChar.Server}"
              Foreground="Blue"           <!-- Color -->
              Width="24"                   <!-- Width -->
              Height="24"                  <!-- Height -->
              Flip="Normal"                <!-- Flip: Normal, Horizontal, Vertical -->
              Rotation="0"                 <!-- Rotation: 0, 90, 180, 270 -->
              Spin="False"                 <!-- Animated spin -->
              SpinDuration="2"/>          <!-- Spin duration in seconds -->
```

### Status Indicators with Color

```xaml
<!-- Service Running (Green) -->
<fa:IconImage Icon="{x:Static fa:IconChar.CircleCheck}" 
              Foreground="Green" 
              Width="16" 
              Height="16"/>

<!-- Service Stopped (Gray) -->
<fa:IconImage Icon="{x:Static fa:IconChar.Circle}" 
              Foreground="Gray" 
              Width="16" 
              Height="16"/>

<!-- Error (Red) -->
<fa:IconImage Icon="{x:Static fa:IconChar.CircleXmark}" 
              Foreground="Red" 
              Width="16" 
              Height="16"/>

<!-- Warning (Orange) -->
<fa:IconImage Icon="{x:Static fa:IconChar.TriangleExclamation}" 
              Foreground="Orange" 
              Width="16" 
              Height="16"/>
```

### Common WPF Config Tool Icons

| Icon | IconChar | Usage |
|------|----------|-------|
| 🔄 | `IconChar.ArrowsRotate` | Refresh/reload |
| ▶️ | `IconChar.Play` | Start service |
| ⏹️ | `IconChar.Stop` | Stop service |
| 💾 | `IconChar.FloppyDisk` | Save configuration |
| 🔑 | `IconChar.Key` | SSH keys |
| 🛡️ | `IconChar.Shield` | Security settings |
| 🌐 | `IconChar.Globe` | Web portal |
| 🖥️ | `IconChar.Server` | Servers |
| 📁 | `IconChar.Folder` | Directories |
| 👤 | `IconChar.User` | User accounts |
| ⚙️ | `IconChar.Gear` | Settings |
| 🧪 | `IconChar.Flask` | Test/validate |
| ✅ | `IconChar.Check` | Success |
| ❌ | `IconChar.CircleXmark` | Error |
| ⚠️ | `IconChar.TriangleExclamation` | Warning |

---

## Complete Icon Mapping

### From Bootstrap Icons to Font Awesome

| Old (Bootstrap) | New (Font Awesome) | IconChar (WPF) |
|----------------|-------------------|----------------|
| `bi-hdd-network` | `fa-network-wired` | `IconChar.NetworkWired` |
| `bi-upload` | `fa-cloud-arrow-up` | `IconChar.CloudArrowUp` |
| `bi-person-circle` | `fa-circle-user` | `IconChar.CircleUser` |
| `bi-geo-alt` | `fa-location-dot` | `IconChar.LocationDot` |
| `bi-shield-check` | `fa-shield-check` | `IconChar.ShieldCheck` |
| `bi-envelope` | `fa-envelope` | `IconChar.Envelope` |
| `bi-cloud-upload` | `fa-cloud-arrow-up` | `IconChar.CloudArrowUp` |
| `bi-info-circle` | `fa-circle-info` | `IconChar.CircleInfo` |
| `bi-file-earmark-text` | `fa-file-lines` | `IconChar.FileLines` |
| `bi-arrow-right-circle` | `fa-circle-arrow-right` | `IconChar.CircleArrowRight` |
| `bi-shield-x` | `fa-shield-xmark` | `IconChar.ShieldXmark` |
| `bi-exclamation-triangle` | `fa-triangle-exclamation` | `IconChar.TriangleExclamation` |
| `bi-check-circle` | `fa-circle-check` | `IconChar.CircleCheck` |
| `bi-list-check` | `fa-list-check` | `IconChar.ListCheck` |
| `bi-x-circle-fill` | `fa-circle-xmark` | `IconChar.CircleXmark` |
| `bi-folder` | `fa-folder` | `IconChar.Folder` |
| `bi-exclamation-circle` | `fa-circle-exclamation` | `IconChar.CircleExclamation` |
| `bi-sticky` | `fa-note-sticky` | `IconChar.NoteSticky` |
| `bi-house` | `fa-house` | `IconChar.House` |
| `bi-person` | `fa-user` | `IconChar.User` |

---

## Button Style Patterns

### Web Portal

```html
<!-- Primary action -->
<button class="btn btn-primary btn-lg">
    <i class="fa-solid fa-cloud-arrow-up"></i> Upload Files
</button>

<!-- Secondary action -->
<button class="btn btn-outline-secondary">
    <i class="fa-solid fa-house"></i> Return to Home
</button>

<!-- Danger action -->
<button class="btn btn-danger">
    <i class="fa-solid fa-trash-can"></i> Delete
</button>

<!-- Success action -->
<button class="btn btn-success">
    <i class="fa-solid fa-check"></i> Confirm
</button>
```

### WPF Config Tool

```xaml
<!-- Primary Button -->
<Button Style="{StaticResource PrimaryButton}">
    <StackPanel Orientation="Horizontal">
        <fa:IconImage Icon="{x:Static fa:IconChar.FloppyDisk}" 
                      Foreground="White" 
                      Width="16" 
                      Height="16" 
                      Margin="0,0,8,0"/>
        <TextBlock Text="Save Configuration" VerticalAlignment="Center"/>
    </StackPanel>
</Button>

<!-- Secondary Button -->
<Button Style="{StaticResource SecondaryButton}">
    <StackPanel Orientation="Horizontal">
        <fa:IconImage Icon="{x:Static fa:IconChar.ArrowsRotate}" 
                      Width="16" 
                      Height="16" 
                      Margin="0,0,8,0"/>
        <TextBlock Text="Refresh" VerticalAlignment="Center"/>
    </StackPanel>
</Button>

<!-- Destructive Button -->
<Button Style="{StaticResource DestructiveButton}">
    <StackPanel Orientation="Horizontal">
        <fa:IconImage Icon="{x:Static fa:IconChar.TrashCan}" 
                      Foreground="White" 
                      Width="16" 
                      Height="16" 
                      Margin="0,0,8,0"/>
        <TextBlock Text="Uninstall Service" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

---

## Best Practices

### General Guidelines

1. **Consistency**: Use the same icon for the same action across the application
2. **Size**: Standard icon size is 16px (can use 14px for compact, 20-24px for emphasis)
3. **Spacing**: Use 8px margin between icon and text
4. **Alignment**: Always `VerticalAlignment="Center"` for icon + text combinations
5. **Color**: Use semantic colors (green=success, red=error, orange=warning, blue=info)

### Web Portal Specific

- Use `fa-solid` style for all icons
- Icons should be inline with text using `<i>` tags
- Use Bootstrap utility classes for sizing (`fa-sm`, `fa-lg`, `fa-2x`)
- Apply color via Bootstrap classes (`text-success`, `text-danger`, `text-warning`)

### WPF Specific

- Use `IconImage` for standalone icons
- Use `IconBlock` for inline text icons
- Reference icons via `IconResources` class for consistency
- Use 16px for standard buttons, 20px for emphasis
- Apply color via `Foreground` property or style binding

---

## Font Awesome Resources

- **Icons Search**: https://fontawesome.com/search
- **Icon Gallery**: https://fontawesome.com/icons
- **FontAwesome.Sharp Docs**: https://github.com/awesome-inc/FontAwesome.Sharp
- **Web Styling**: https://fontawesome.com/docs/web/style

---

## Updating Icons

### Adding New Icons to Web Portal

1. Find icon on Font Awesome website
2. Use the icon name (e.g., `arrow-up-from-bracket`)
3. Add to HTML: `<i class="fa-solid fa-arrow-up-from-bracket"></i>`

### Adding New Icons to WPF

1. Find icon on Font Awesome website
2. Locate corresponding `IconChar` enum value (e.g., `IconChar.ArrowUpFromBracket`)
3. Optionally add to `IconResources.cs`:
   ```csharp
   public static IconChar UploadFromDevice => IconChar.ArrowUpFromBracket;
   ```
4. Use in XAML:
   ```xaml
   <fa:IconImage Icon="{x:Static resources:IconResources.UploadFromDevice}"/>
   ```

---

## Troubleshooting

### Web Portal: Icons Not Showing

**Symptoms**: Icons appear as empty squares or missing

**Solutions**:
1. Verify Font Awesome files exist in `wwwroot/lib/fontawesome/`
2. Check browser console for 404 errors on font files
3. Ensure `all.min.css` is referenced in `_Layout.cshtml`
4. Clear browser cache and hard refresh (Ctrl+F5)

### WPF: Icons Not Showing

**Symptoms**: Icons don't appear in WPF application

**Solutions**:
1. Verify `FontAwesome.Sharp` NuGet package is installed
2. Check namespace declaration: `xmlns:fa="http://schemas.awesome.incremented/wpf/xaml/fontawesome.sharp"`
3. Rebuild the solution to restore NuGet packages
4. Verify `IconChar` enum value is correct (case-sensitive)

### Performance Issues

**Web Portal**: The `all.min.css` file is ~80KB gzipped. For optimization:
- Use only `fontawesome.min.css` + `solid.min.css` if you don't need other styles
- Font files are loaded on-demand

**WPF**: FontAwesome.Sharp is lightweight and has minimal impact on startup time.

---

**Last Updated**: October 8, 2025  
**Font Awesome Version**: 6.x  
**FontAwesome.Sharp Version**: 6.3.0  
**Maintained By**: ZL File Relay Development Team