# Font Awesome Pro Setup Instructions

## Self-Hosted Font Awesome Pro Integration

Since this application may be deployed in air-gapped/DMZ environments, Font Awesome is self-hosted rather than using a CDN.

## Installation Steps

### 1. Download Font Awesome Pro

1. Go to https://fontawesome.com/download
2. Log in with your Font Awesome Pro account
3. Download **Font Awesome Pro for Web** (latest version)
4. Extract the downloaded zip file

### 2. Copy Files to Project

From the extracted Font Awesome package, copy the following folders into `src/ZLFileRelay.WebPortal/wwwroot/lib/fontawesome/`:

```
fontawesome/
├── css/
│   ├── all.min.css          (Required - includes all styles)
│   ├── fontawesome.min.css  (Core styles)
│   ├── solid.min.css        (Solid icons)
│   ├── regular.min.css      (Regular icons)
│   └── brands.min.css       (Brand icons - optional)
└── webfonts/
    ├── fa-solid-900.woff2    (Solid font files)
    ├── fa-solid-900.ttf
    ├── fa-regular-400.woff2  (Regular font files)
    ├── fa-regular-400.ttf
    └── fa-brands-400.woff2   (Brand font files - optional)
        fa-brands-400.ttf
```

### 3. What to Copy

**Minimum Required:**
- `css/all.min.css` - Main CSS file (includes all icon styles)
- `webfonts/` folder - All font files (*.woff2, *.woff, *.ttf, *.eot)

**Recommended:**
Copy the entire `css/` and `webfonts/` folders for maximum compatibility.

### 4. Directory Structure After Setup

```
wwwroot/lib/
├── bootstrap/
├── jquery/
├── fontawesome/                 ← Create this folder
│   ├── css/
│   │   └── all.min.css         ← Required
│   ├── webfonts/               ← Required (all font files)
│   └── LICENSE.txt             ← Include Font Awesome license
└── FONTAWESOME_SETUP.md        ← This file
```

### 5. Verify Installation

After copying files, the application will automatically use Font Awesome icons instead of Bootstrap Icons.

Check that these files exist:
- `wwwroot/lib/fontawesome/css/all.min.css`
- `wwwroot/lib/fontawesome/webfonts/fa-solid-900.woff2`

### 6. License Compliance

Since you have a Font Awesome Pro subscription:
- Include the `LICENSE.txt` file from the Font Awesome package
- Font Awesome Pro licenses allow self-hosting
- Keep your subscription active for updates and support

## Icon Migration Reference

The application has been migrated from Bootstrap Icons to Font Awesome:

| Bootstrap Icon | Font Awesome | Usage |
|---------------|--------------|-------|
| `bi-hdd-network` | `fa-solid fa-network-wired` | Product branding |
| `bi-upload` | `fa-solid fa-cloud-arrow-up` | Upload actions |
| `bi-person-circle` | `fa-solid fa-circle-user` | User profile |
| `bi-shield-check` | `fa-solid fa-shield-check` | Security |
| `bi-envelope` | `fa-solid fa-envelope` | Contact email |

## Updates

When updating Font Awesome to a newer version:
1. Download the latest Font Awesome Pro package
2. Replace the `css/` and `webfonts/` folders
3. Test the application to ensure all icons display correctly
4. Update this README with the new version number

## Current Version

Font Awesome Pro Version: **6.x** (Update this after installation)

## Troubleshooting

**Icons not showing:**
- Verify `all.min.css` exists in `wwwroot/lib/fontawesome/css/`
- Verify `webfonts/` folder contains all font files
- Check browser console for 404 errors
- Ensure paths in `_Layout.cshtml` are correct

**Performance:**
- The `all.min.css` file includes all icon styles (~80KB gzipped)
- Font files are loaded on-demand by the browser
- Consider using only specific style files (solid.min.css) if you want to optimize size

## Alternative: Font Awesome Free

If you only need basic icons and don't have a Pro subscription, you can use Font Awesome Free:
1. Download from: https://fontawesome.com/download (Free for Web)
2. Follow the same installation steps above
3. Some Pro icons in the app may not be available (will fall back to solid style)
