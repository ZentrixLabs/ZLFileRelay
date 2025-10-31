# Font Awesome Free Fonts for WPF

This folder contains Font Awesome Free font files for use in the WPF Config Tool.

## Installation Instructions

### Step 1: Download Font Awesome Free for Desktop

1. Go to https://fontawesome.com/download
2. **No account needed!** Font Awesome Free is open source
3. Download **"Font Awesome Free for Desktop"**
4. Extract the ZIP file

### Step 2: Copy Font Files

From the extracted package, copy these font files to this folder (`src/ZLFileRelay.ConfigTool/Fonts/`):

```
From Desktop Package:           Copy To This Folder:
├── otfs/                       
│   ├── Font Awesome 7 Free-Solid-900.otf      → Font Awesome 7 Free-Solid-900.otf
│   ├── Font Awesome 7 Free-Regular-400.otf    → Font Awesome 7 Free-Regular-400.otf
│   └── Font Awesome 7 Brands-Regular-400.otf  → Font Awesome 7 Brands-Regular-400.otf (optional)
```

**Required Files:**
- `Font Awesome 7 Free-Solid-900.otf` - Solid icons (main set - 2,000+ icons)
- `Font Awesome 7 Free-Regular-400.otf` - Regular/outline icons (~150 icons)

**Optional Files:**
- `Font Awesome 7 Brands-Regular-400.otf` - Brand logos (500+ brands)

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
├── Font Awesome 7 Free-Solid-900.otf      ← Required
├── Font Awesome 7 Free-Regular-400.otf    ← Required
└── Font Awesome 7 Brands-Regular-400.otf  ← Optional
```

### Troubleshooting

**Icons not showing:**
1. Verify font files are in this `Fonts/` folder
2. Check file names match exactly (case-sensitive):
   - Must say **"Free"** not "Pro"
   - Must be **".otf"** files
3. Run `dotnet clean` then `dotnet build`
4. Check that files are marked as `EmbeddedResource` in `.csproj`

**Build errors:**
- Ensure font files are valid .otf files
- Check file sizes:
  - Solid: ~400 KB
  - Regular: ~100 KB
  - Brands: ~200 KB

## License

Font Awesome Free is open source!

- **Fonts:** SIL OFL 1.1 License (can be distributed freely)
- **Icons:** CC BY 4.0 License
- **Code:** MIT License

**✅ These fonts CAN be committed to git**  
**✅ No subscription or account required**  
**✅ Free forever**

**See:** https://fontawesome.com/license/free

## Migration from Font Awesome Pro

If you previously had Font Awesome Pro fonts installed:

1. Delete the old Pro font files:
   - `Font Awesome 6 Pro-Solid-900.otf` or `Font Awesome 7 Pro-Solid-900.otf`
   - `Font Awesome 6 Pro-Regular-400.otf` or `Font Awesome 7 Pro-Regular-400.otf`
   - etc.

2. Download and copy the Free versions (see Step 1-2 above)

3. Clean and rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

All icons used in ZLFileRelay ConfigTool are available in Font Awesome Free!

## Available Icons

Font Awesome Free includes:
- ✅ 2,000+ free icons (Solid style)
- ✅ ~150 icons in Regular style
- ✅ 500+ brand logos
- ✅ All icons used by ZLFileRelay ConfigTool

See all available icons: https://fontawesome.com/search?o=r&m=free
