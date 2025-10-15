# Font Awesome Pro - Quick Start

## 🎯 What You Need to Download

Go to **https://fontawesome.com/download** and get both:

### 1. Font Awesome Pro for Web
- ✅ Includes: `css/` and `webfonts/` folders
- 📂 Copy to: `src/ZLFileRelay.WebPortal/wwwroot/lib/fontawesome/`
- 🎨 Gives you: Pro icons in the web portal

### 2. Font Awesome Pro for Desktop  
- ✅ Includes: `.otf` font files in `otfs/` folder
- 📂 Copy to: `src/ZLFileRelay.ConfigTool/Fonts/`
- 🎨 Gives you: Pro icons (Light, Duotone) in WPF config tool

---

## 📋 Copy Checklist

### Web Portal Files
```
From "for Web" download → To your project:

css/all.min.css              → wwwroot/lib/fontawesome/css/all.min.css
webfonts/*.woff2             → wwwroot/lib/fontawesome/webfonts/
webfonts/*.ttf               → wwwroot/lib/fontawesome/webfonts/
```

### Desktop Config Tool Files
```
From "for Desktop" download → To your project:

otfs/Font Awesome 6 Pro-Solid-900.otf      → Fonts/Font Awesome 6 Pro-Solid-900.otf
otfs/Font Awesome 6 Pro-Regular-400.otf    → Fonts/Font Awesome 6 Pro-Regular-400.otf
otfs/Font Awesome 6 Pro-Light-300.otf      → Fonts/Font Awesome 6 Pro-Light-300.otf
otfs/Font Awesome 6 Brands-Regular-400.otf → Fonts/Font Awesome 6 Brands-Regular-400.otf
```

---

## 🚀 After Copying Files

```powershell
# Restore packages and build
dotnet restore
dotnet build

# Test web portal
cd src/ZLFileRelay.WebPortal
dotnet run

# Test config tool
cd src/ZLFileRelay.ConfigTool
dotnet run
```

---

## ✨ Pro Features You Get

### Web Portal
- 2,000+ Pro icons
- All icon styles (Solid, Regular, Light, Duotone)
- Self-hosted (no CDN needed)
- Works in air-gapped environments

### Desktop Config Tool
- Light style icons (thin, modern look)
- Duotone icons (two-tone colors)
- Regular style with better coverage
- All embedded in the app (no installation needed)

---

## 📚 Full Documentation

- **Setup Guide (Web)**: `src/ZLFileRelay.WebPortal/wwwroot/lib/FONTAWESOME_SETUP.md`
- **Setup Guide (Desktop)**: `FONTAWESOME_PRO_DESKTOP_SETUP.md`
- **Usage Reference**: `docs/ICON_REFERENCE.md`
- **Migration Summary**: `FONTAWESOME_MIGRATION_COMPLETE.md`

---

## 🎨 Quick Examples

### Web (HTML/Razor)
```html
<!-- Standard icon -->
<i class="fa-solid fa-cloud-arrow-up"></i> Upload

<!-- With color -->
<i class="fa-solid fa-circle-check text-success"></i> Success

<!-- Light style (Pro) -->
<i class="fa-light fa-gear"></i> Settings
```

### Desktop (WPF/XAML)
```xaml
<!-- Namespace -->
xmlns:fa="http://schemas.awesome.incremented/wpf/xaml/fontawesome.sharp"

<!-- Solid icon (Free) -->
<fa:IconImage Icon="{x:Static fa:IconChar.Save}" Width="20" Height="20"/>

<!-- Light icon (Pro) -->
<fa:IconImage Icon="{x:Static fa:IconChar.Save}" 
              IconFont="Light" 
              Width="20" 
              Height="20"/>

<!-- Duotone icon (Pro) -->
<fa:IconImage Icon="{x:Static fa:IconChar.Server}" 
              IconFont="Duotone" 
              Foreground="DodgerBlue"
              Width="24" 
              Height="24"/>
```

---

## ⚡ TL;DR

1. Download 2 packages from fontawesome.com
2. Copy files to 2 folders
3. Run `dotnet build`
4. Enjoy Pro icons in both web and desktop! 🎉

**That's it!** Everything else is already configured.

---

**Questions?** See the detailed guides listed above or check https://fontawesome.com/docs
