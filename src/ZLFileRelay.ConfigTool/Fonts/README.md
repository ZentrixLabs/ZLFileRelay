# Font Awesome Pro Fonts for WPF

This folder contains Font Awesome Pro font files for use in the WPF Config Tool.

## Installation Instructions

### Step 1: Download Font Awesome Pro for Desktop

1. Go to https://fontawesome.com/download
2. Log in with your Font Awesome Pro account
3. Download **"Font Awesome Pro for Desktop"**
4. Extract the ZIP file

### Step 2: Copy Font Files

From the extracted package, copy these font files to this folder (`src/ZLFileRelay.ConfigTool/Fonts/`):

```
From Desktop Package:           Copy To This Folder:
├── otfs/                       
│   ├── Font Awesome 6 Pro-Solid-900.otf      → Font Awesome 6 Pro-Solid-900.otf
│   ├── Font Awesome 6 Pro-Regular-400.otf    → Font Awesome 6 Pro-Regular-400.otf
│   ├── Font Awesome 6 Pro-Light-300.otf      → Font Awesome 6 Pro-Light-300.otf (optional)
│   └── Font Awesome 6 Brands-Regular-400.otf → Font Awesome 6 Brands-Regular-400.otf (optional)
```

**Required Files:**
- `Font Awesome 6 Pro-Solid-900.otf` - Solid icons (main set)
- `Font Awesome 6 Pro-Regular-400.otf` - Regular/outline icons

**Optional Files:**
- `Font Awesome 6 Pro-Light-300.otf` - Light weight icons
- `Font Awesome 6 Brands-Regular-400.otf` - Brand logos

### Step 3: Verify Files Are Embedded

The `.csproj` file should already have these configured as `EmbeddedResource`. After copying the fonts:

```bash
dotnet clean
dotnet build
```

### File Structure After Setup

```
src/ZLFileRelay.ConfigTool/Fonts/
├── README.md                              ← This file
├── Font Awesome 6 Pro-Solid-900.otf       ← Required
├── Font Awesome 6 Pro-Regular-400.otf     ← Required
├── Font Awesome 6 Pro-Light-300.otf       ← Optional
└── Font Awesome 6 Brands-Regular-400.otf  ← Optional
```

### Troubleshooting

**Icons not showing:**
1. Verify font files are in this `Fonts/` folder
2. Check file names match exactly (case-sensitive)
3. Run `dotnet clean` then `dotnet build`
4. Check that files are marked as `EmbeddedResource` in `.csproj`

**Build errors:**
- Ensure font files are valid .otf files
- Check file sizes (each should be ~200-400 KB)

## License

Font Awesome Pro fonts are included under your Font Awesome Pro subscription license. 
Do not distribute these font files publicly. They should be added to `.gitignore`.

**See**: https://fontawesome.com/license
