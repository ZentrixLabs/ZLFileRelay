# Font Awesome Pro for Desktop (WPF) Setup Guide

Complete guide for using Font Awesome Pro icons in the ZL File Relay Config Tool.

## Overview

The WPF Config Tool uses **FontAwesome.Sharp** NuGet package, which by default includes Font Awesome **Free** icons. To unlock Font Awesome **Pro** icons (Light, Duotone, etc.), you need to add the Pro font files as embedded resources.

---

## Quick Setup (3 Steps)

### Step 1: Download Font Awesome Pro for Desktop

1. Go to https://fontawesome.com/download
2. Log in with your Font Awesome Pro account
3. Download **"Font Awesome Pro for Desktop"**
4. Extract the ZIP file

### Step 2: Copy Font Files

From the extracted package, navigate to the `otfs/` folder and copy these files:

**Copy from:**
```
fontawesome-pro-desktop/otfs/
├── Font Awesome 6 Pro-Solid-900.otf       ← Required
├── Font Awesome 6 Pro-Regular-400.otf     ← Required
├── Font Awesome 6 Pro-Light-300.otf       ← Optional (Pro feature)
└── Font Awesome 6 Brands-Regular-400.otf  ← Optional
```

**Copy to:**
```
src/ZLFileRelay.ConfigTool/Fonts/
```

**Note:** The exact file names may vary slightly depending on Font Awesome version. Use whatever names the download provides.

### Step 3: Build

```powershell
cd src/ZLFileRelay.ConfigTool
dotnet clean
dotnet build
```

The fonts will be embedded into the application automatically!

---

## What You Get with Pro

### Free vs Pro Icon Comparison

| Feature | Free | Pro |
|---------|------|-----|
| **Solid Icons** | ✅ Yes (~1,900) | ✅ Yes (~2,000+) |
| **Regular Icons** | ⚠️ Limited (~150) | ✅ Full Set (~2,000+) |
| **Light Icons** | ❌ No | ✅ Yes |
| **Duotone Icons** | ❌ No | ✅ Yes |
| **Thin Icons** | ❌ No | ✅ Yes |
| **Sharp Icons** | ❌ No | ✅ Yes |
| **Brand Icons** | ✅ Yes | ✅ Yes |

### Pro-Only Icon Styles in WPF

Once Pro fonts are installed, you can use additional styles:

```xaml
xmlns:fa="http://schemas.awesome.incremented/wpf/xaml/fontawesome.sharp"

<!-- Solid (Available in Free) -->
<fa:IconImage Icon="{x:Static fa:IconChar.Save}" 
              IconFont="Solid" 
              Width="20" Height="20"/>

<!-- Regular (Better in Pro) -->
<fa:IconImage Icon="{x:Static fa:IconChar.Save}" 
              IconFont="Regular" 
              Width="20" Height="20"/>

<!-- Light (Pro Only) -->
<fa:IconImage Icon="{x:Static fa:IconChar.Save}" 
              IconFont="Light" 
              Width="20" Height="20"/>

<!-- Duotone (Pro Only) - Two-tone colors -->
<fa:IconImage Icon="{x:Static fa:IconChar.Save}" 
              IconFont="Duotone" 
              Foreground="Blue"
              Width="20" Height="20"/>
```

---

## Font Files Explained

### Required Files (Minimum)

**Font Awesome 6 Pro-Solid-900.otf** (Required)
- Main icon set
- ~2,000+ filled icons
- Used for most UI elements
- Size: ~200-300 KB

**Font Awesome 6 Pro-Regular-400.otf** (Required)
- Outline/hollow icons
- Great for subtle UI elements
- Size: ~200-300 KB

### Optional Files (Enhanced Features)

**Font Awesome 6 Pro-Light-300.otf** (Optional)
- Thin outline icons
- Modern, minimal look
- Size: ~200 KB

**Font Awesome 6 Brands-Regular-400.otf** (Optional)
- Brand/logo icons (GitHub, Windows, etc.)
- Size: ~150 KB

**Total Size:** ~600 KB - 1 MB (all files)

---

## How It Works

### Embedded Resources

The `.csproj` file is configured to embed the font files:

```xml
<ItemGroup>
  <EmbeddedResource Include="Fonts\Font Awesome 6 Pro-Solid-900.otf" />
  <EmbeddedResource Include="Fonts\Font Awesome 6 Pro-Regular-400.otf" />
  <EmbeddedResource Include="Fonts\Font Awesome 6 Pro-Light-300.otf" />
  <EmbeddedResource Include="Fonts\Font Awesome 6 Brands-Regular-400.otf" />
</ItemGroup>
```

When you build the project:
1. Font files are embedded into the `.dll`
2. FontAwesome.Sharp loads fonts from embedded resources
3. Icons display using Pro features automatically

**No runtime font installation required!** The fonts are packaged with your application.

---

## Usage Examples

### Basic Icon (Solid)

```xaml
xmlns:fa="http://schemas.awesome.incremented/wpf/xaml/fontawesome.sharp"

<fa:IconImage Icon="{x:Static fa:IconChar.CloudArrowUp}" 
              Width="24" 
              Height="24" 
              Foreground="DodgerBlue"/>
```

### Using IconResources

```xaml
xmlns:resources="clr-namespace:ZLFileRelay.ConfigTool.Resources"

<fa:IconImage Icon="{x:Static resources:IconResources.Save}" 
              Width="20" 
              Height="20"/>
```

### Button with Icon (Pro Regular)

```xaml
<Button Style="{StaticResource PrimaryButton}">
    <StackPanel Orientation="Horizontal">
        <fa:IconImage Icon="{x:Static fa:IconChar.Gear}" 
                      IconFont="Regular"
                      Foreground="White" 
                      Width="16" 
                      Height="16" 
                      Margin="0,0,8,0"/>
        <TextBlock Text="Settings" VerticalAlignment="Center"/>
    </StackPanel>
</Button>
```

### Status Indicator with Light Style

```xaml
<!-- Success (Light style for subtle look) -->
<StackPanel Orientation="Horizontal">
    <fa:IconImage Icon="{x:Static fa:IconChar.CircleCheck}" 
                  IconFont="Light"
                  Foreground="Green" 
                  Width="18" 
                  Height="18" 
                  Margin="0,0,5,0"/>
    <TextBlock Text="Service Running" Foreground="Green" FontWeight="Medium"/>
</StackPanel>
```

---

## Verification

### Check if Pro Fonts Are Working

After building, test if Pro features are available:

1. **Run the Config Tool**
2. **Look for icons** - they should display correctly
3. **Try different styles:**
   - Change `IconFont="Solid"` to `IconFont="Light"`
   - If Light icons show, Pro fonts are working! ✅

### Troubleshooting

#### Icons Show as Squares/Missing

**Problem:** Icons display as empty squares or don't show

**Solutions:**
1. ✅ Verify font files are in `Fonts/` folder
2. ✅ Check file names match exactly (case-sensitive)
3. ✅ Ensure files are `.otf` format (not `.ttf`)
4. ✅ Run `dotnet clean` then `dotnet build`
5. ✅ Check `.csproj` has `<EmbeddedResource Include="Fonts\...">` lines

#### Build Warnings About Missing Files

**Problem:** Build shows warnings like "File not found: Font Awesome 6 Pro-Solid-900.otf"

**Solution:** 
- Font files haven't been copied yet
- This is expected until you download and copy the fonts
- App will still build, but will fall back to Free icons only

#### Light/Duotone Styles Don't Work

**Problem:** Setting `IconFont="Light"` shows solid icons instead

**Solution:**
- Light font file not included
- Copy `Font Awesome 6 Pro-Light-300.otf` to `Fonts/` folder
- Rebuild the application

---

## Deployment

### Distribution

Font files are **embedded in the application**:
- ✅ No separate font installation needed
- ✅ Users don't need Font Awesome Pro subscription
- ✅ Works on any Windows machine
- ✅ Self-contained executable

### License Compliance

**Your Font Awesome Pro subscription allows:**
- ✅ Embedding Pro fonts in desktop applications
- ✅ Distributing embedded fonts with your app
- ✅ Using Pro icons commercially

**Requirements:**
- Keep your Font Awesome Pro subscription active
- Don't redistribute raw font files separately
- Don't commit font files to public repositories (`.gitignore` configured)

**See:** https://fontawesome.com/license

---

## Comparison: Free vs Pro in Practice

### Example: Settings Button

**With Free:**
```xaml
<!-- Solid only -->
<fa:IconImage Icon="{x:Static fa:IconChar.Gear}" Width="20" Height="20"/>
```

**With Pro:**
```xaml
<!-- Choose your style -->
<fa:IconImage Icon="{x:Static fa:IconChar.Gear}" IconFont="Light" Width="20" Height="20"/>
<fa:IconImage Icon="{x:Static fa:IconChar.Gear}" IconFont="Regular" Width="20" Height="20"/>
<fa:IconImage Icon="{x:Static fa:IconChar.Gear}" IconFont="Solid" Width="20" Height="20"/>
```

### Visual Difference

| Style | Look | Best For |
|-------|------|----------|
| **Solid** | Filled, bold | Primary actions, emphasis |
| **Regular** | Outline, medium weight | Secondary UI, balanced look |
| **Light** | Thin outline, minimal | Modern, clean interfaces |
| **Duotone** | Two-tone color | Visual hierarchy, branding |

---

## File Checklist

After setup, your folder structure should look like:

```
src/ZLFileRelay.ConfigTool/
├── Fonts/
│   ├── .gitignore                               ← Prevents committing fonts
│   ├── README.md                                ← Setup instructions
│   ├── Font Awesome 6 Pro-Solid-900.otf        ← Your Pro fonts
│   ├── Font Awesome 6 Pro-Regular-400.otf
│   ├── Font Awesome 6 Pro-Light-300.otf
│   └── Font Awesome 6 Brands-Regular-400.otf
├── Resources/
│   └── IconResources.cs                         ← Icon constants
├── App.xaml
├── App.xaml.cs
└── ZLFileRelay.ConfigTool.csproj               ← Embeds fonts
```

---

## Summary

### ✅ What's Configured
- [x] FontAwesome.Sharp NuGet package added
- [x] `Fonts/` folder created
- [x] `.csproj` configured for embedded resources
- [x] `.gitignore` prevents committing Pro fonts
- [x] `IconResources.cs` updated with Font Awesome icons

### ⚠️ What You Need to Do
- [ ] Download Font Awesome Pro for Desktop
- [ ] Copy `.otf` files to `Fonts/` folder
- [ ] Run `dotnet build`
- [ ] Enjoy your Pro icons! 🎉

### 📖 Next Steps
- Update existing XAML views to use `<fa:IconImage>` controls
- Experiment with different icon styles (Light, Regular, Solid)
- See `docs/ICON_REFERENCE.md` for complete usage guide

---

**Last Updated:** October 8, 2025  
**FontAwesome.Sharp Version:** 6.3.0  
**Font Awesome Pro Version:** 6.x  
**Status:** Ready for Pro font installation
