# Icon Reference Guide

Quick reference for using icons in ZL File Relay ConfigTool.

## Using Icons in XAML

### Method 1: Direct Icon Usage
```xaml
<TextBlock FontFamily="Segoe MDL2 Assets" Text="&#xE72C;" FontSize="16"/>
```

### Method 2: Reference IconResources (Recommended)
```csharp
// In C# code-behind
using ZLFileRelay.ConfigTool.Resources;

myTextBlock.Text = IconResources.Refresh;
myTextBlock.FontFamily = new FontFamily("Segoe MDL2 Assets");
```

### Method 3: Button with Icon + Text
```xaml
<Button Style="{StaticResource PrimaryButton}">
    <StackPanel Orientation="Horizontal">
        <TextBlock FontFamily="Segoe MDL2 Assets" 
                   Text="&#xE74E;" 
                   FontSize="16" 
                   Margin="0,0,8,0" 
                   VerticalAlignment="Center"/>
        <TextBlock Text="Save Configuration" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

## Icon Catalog

### Service & Actions
| Icon | Unicode | Property | Usage |
|------|---------|----------|-------|
| 🔄 | E72C | `Refresh` | Refresh/reload actions |
| 📥 | E896 | `Download` | Install/download |
| 🗑️ | E74D | `Delete` | Delete/uninstall |
| ▶️ | E768 | `Play` | Start service/process |
| ⏹️ | E71A | `Stop` | Stop service/process |
| 💾 | E74E | `Save` | Save configuration |
| 🔧 | E90F | `Repair` | Fix/repair actions |

### Security & Authentication
| Icon | Unicode | Property | Usage |
|------|---------|----------|-------|
| 🔑 | E192 | `Key` | SSH keys, authentication |
| 🔒 | E72E | `Shield` | Security settings |
| 🔓 | E785 | `Unlock` | Permissions, access |
| 📜 | EB95 | `Certificate` | SSL certificates |
| 👤 | E77B | `Contact` | User accounts |
| 👨‍💼 | E7EF | `Admin` | Admin accounts |

### Network & Files
| Icon | Unicode | Property | Usage |
|------|---------|----------|-------|
| 🌐 | E774 | `Globe` | Web portal, internet |
| 🖥️ | E968 | `Server` | Remote servers |
| 📁 | E8B7 | `Folder` | Directories |
| 📂 | E838 | `FolderOpen` | Browse folders |
| 📄 | E8A5 | `Document` | Files |
| ☁️⬆️ | E753 | `CloudUpload` | Upload operations |

### Status & Feedback
| Icon | Unicode | Property | Usage |
|------|---------|----------|-------|
| ✅ | E73E | `CheckMark` | Success, valid |
| ⚠️ | E7BA | `Warning` | Warnings |
| ❌ | E783 | `Error` | Errors |
| ℹ️ | E946 | `Info` | Information |
| ⚪ | F136 | `StatusCircle` | Neutral status |
| 🟢 | F13E | `StatusCircleCheckmark` | Active/running |

### UI Elements
| Icon | Unicode | Property | Usage |
|------|---------|----------|-------|
| 👁️ | E890 | `View` | View/preview |
| 📋 | E8C8 | `Copy` | Copy to clipboard |
| 🧪 | EA46 | `TestBeaker` | Test/validate |
| ⚙️ | E713 | `Settings` | Settings/config |
| 🏠 | E80F | `Home` | Dashboard/home |
| 🔍 | E721 | `Search` | Search |
| 📤 | EDE1 | `Export` | Export data |
| 📥 | E8B5 | `Import` | Import data |
| ➕ | E710 | `Add` | Add new item |
| ➖ | E738 | `Remove` | Remove item |
| ✏️ | E70F | `Edit` | Edit item |
| 📋 | E8FD | `List` | Lists/logs |
| ➡️ | E76C | `ChevronRight` | Navigate forward |
| ⬇️ | E70D | `ChevronDown` | Expand |
| ⋯ | E712 | `More` | More options |

## Button Style Guide

### Primary Button (Accent)
Use for main action in current context:
```xaml
<Button Style="{StaticResource PrimaryButton}">
    <StackPanel Orientation="Horizontal">
        <TextBlock FontFamily="Segoe MDL2 Assets" Text="&#xE74E;" FontSize="16" Margin="0,0,8,0"/>
        <TextBlock Text="Save Configuration"/>
    </StackPanel>
</Button>
```

**When to use:**
- Save/Apply actions
- Start/Connect actions  
- Generate/Create actions
- Primary action per tab

### Secondary Button (Default)
Use for supporting actions:
```xaml
<Button Style="{StaticResource SecondaryButton}">
    <StackPanel Orientation="Horizontal">
        <TextBlock FontFamily="Segoe MDL2 Assets" Text="&#xE72C;" FontSize="16" Margin="0,0,8,0"/>
        <TextBlock Text="Refresh"/>
    </StackPanel>
</Button>
```

**When to use:**
- Refresh/Reload
- Test/Validate
- Browse/Select
- View/Copy
- Most common button type

### Destructive Button (Red)
Use for dangerous/irreversible actions:
```xaml
<Button Style="{StaticResource DestructiveButton}">
    <StackPanel Orientation="Horizontal">
        <TextBlock FontFamily="Segoe MDL2 Assets" Text="&#xE74D;" FontSize="16" Margin="0,0,8,0"/>
        <TextBlock Text="Uninstall Service"/>
    </StackPanel>
</Button>
```

**When to use:**
- Delete/Uninstall
- Remove/Clear (permanent)
- Stop (critical services)
- Any irreversible action

### Subtle Button (Minimal)
Use for low-priority actions:
```xaml
<Button Style="{StaticResource SubtleButton}">
    <StackPanel Orientation="Horizontal">
        <TextBlock FontFamily="Segoe MDL2 Assets" Text="&#xE894;" FontSize="16" Margin="0,0,8,0"/>
        <TextBlock Text="Clear"/>
    </StackPanel>
</Button>
```

**When to use:**
- Clear log
- Collapse section
- Dismiss message
- Tertiary actions

## Color Coding for Status

### Service Status
```csharp
// Running (Green)
StatusBarServiceIcon.Foreground = System.Windows.Media.Brushes.Green;
StatusBarServiceIcon.Text = "\uF13E"; // StatusCircleCheckmark

// Stopped (Gray)
StatusBarServiceIcon.Foreground = System.Windows.Media.Brushes.Gray;
StatusBarServiceIcon.Text = "\uF136"; // StatusCircle

// Not Installed (Orange)
StatusBarServiceIcon.Foreground = System.Windows.Media.Brushes.Orange;
StatusBarServiceIcon.Text = "\uE783"; // Warning
```

### Validation Status
```csharp
// Valid (Green)
icon.Text = "\uE73E"; // CheckMark
icon.Foreground = Brushes.Green;

// Warning (Orange)
icon.Text = "\uE7BA"; // Warning
icon.Foreground = Brushes.Orange;

// Error (Red)
icon.Text = "\uE783"; // Error
icon.Foreground = Brushes.Red;
```

## Best Practices

1. **Consistency**: Use the same icon for the same action across the app
2. **Size**: Standard icon size is 16px (can use 14px for compact, 20px for emphasis)
3. **Spacing**: 8px margin between icon and text
4. **Alignment**: Always VerticalAlignment="Center" for icon + text
5. **Color**: Let button styles handle color (except status indicators)
6. **Tooltips**: Add tooltips to icon-only buttons

## Adding New Icons

1. Find icon on: https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font
2. Note the Unicode value (e.g., E72C)
3. Add to `IconResources.cs`:
   ```csharp
   public static string MyNewIcon => "\uE72C";
   ```
4. Add to this reference guide
5. Use in XAML:
   ```xaml
   Text="&#xE72C;"
   ```

## Common Patterns

### Icon Button (Standard)
```xaml
<Button Style="{StaticResource SecondaryButton}">
    <StackPanel Orientation="Horizontal">
        <TextBlock FontFamily="Segoe MDL2 Assets" Text="&#xE72C;" FontSize="16" Margin="0,0,8,0" VerticalAlignment="Center"/>
        <TextBlock Text="Action Name" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

### Icon-Only Button (Compact)
```xaml
<Button Style="{StaticResource SecondaryButton}" 
        Width="32" Height="32" 
        Padding="0"
        ToolTip="Action Name">
    <TextBlock FontFamily="Segoe MDL2 Assets" Text="&#xE72C;" FontSize="16"/>
</Button>
```

### Status with Icon
```xaml
<StackPanel Orientation="Horizontal">
    <TextBlock FontFamily="Segoe MDL2 Assets" Text="&#xE73E;" Foreground="Green" Margin="0,0,5,0"/>
    <TextBlock Text="Configuration Valid" Foreground="Green"/>
</StackPanel>
```

---

**Last Updated:** October 8, 2025  
**Reference:** Segoe MDL2 Assets Font  
**Source:** `src/ZLFileRelay.ConfigTool/Resources/IconResources.cs`

