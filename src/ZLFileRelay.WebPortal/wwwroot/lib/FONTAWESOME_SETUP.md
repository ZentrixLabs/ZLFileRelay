# Font Awesome Free Setup for Web Portal

This guide explains how to set up Font Awesome Free for the ZLFileRelay Web Portal.

## Overview

Font Awesome Free is **open source** and includes 2,000+ free icons - more than enough for this project!

- ✅ **No account required**
- ✅ **No subscription cost**
- ✅ **Can be committed to git**
- ✅ **SIL OFL 1.1 License** (fonts)
- ✅ **CC BY 4.0 License** (icons)
- ✅ **MIT License** (code)

---

## Quick Setup

### Step 1: Download Font Awesome Free

1. Go to https://fontawesome.com/download
2. Click **"Free for Web"** download button (no account needed)
3. Extract the ZIP file

### Step 2: Copy Files to Project

Copy the contents of the extracted `fontawesome-free-6.x.x-web` folder to:

```
src/ZLFileRelay.WebPortal/wwwroot/lib/fontawesome/
```

The structure should be:

```
wwwroot/lib/fontawesome/
├── css/
│   ├── all.min.css           (Main stylesheet - includes all styles)
│   ├── fontawesome.min.css   (Core Font Awesome CSS)
│   ├── solid.min.css         (Solid style)
│   ├── regular.min.css       (Regular style)
│   └── brands.min.css        (Brand logos)
├── webfonts/
│   ├── fa-solid-900.woff2    (Solid font files)
│   ├── fa-solid-900.ttf
│   ├── fa-regular-400.woff2  (Regular font files)
│   ├── fa-regular-400.ttf
│   ├── fa-brands-400.woff2   (Brand font files)
│   └── fa-brands-400.ttf
└── js/
    ├── all.min.js            (Optional JavaScript)
    └── fontawesome.min.js
```

### Step 3: Verify in _Layout.cshtml

The stylesheet reference should already be in `Pages/Shared/_Layout.cshtml`:

```html
<link rel="stylesheet" href="~/lib/fontawesome/css/all.min.css" />
```

### Step 4: Test

Run the web portal and verify icons are showing:

```bash
dotnet run --project src/ZLFileRelay.WebPortal
```

Navigate to http://localhost:8080 and check that icons display correctly.

---

## Alternative: CDN Setup (Not Recommended for DMZ)

If you're NOT in a DMZ environment, you can use the Font Awesome CDN:

```html
<!-- In _Layout.cshtml -->
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css" />
```

**Note:** CDN won't work in air-gapped/DMZ environments. Use local files instead.

---

## Icons Used in Web Portal

All icons used in ZLFileRelay Web Portal are available in Font Awesome Free:

### Solid Style Icons (fa-solid)
- ✅ `fa-circle-check` - Success indicator
- ✅ `fa-triangle-exclamation` - Warning
- ✅ `fa-clipboard-list` - Lists
- ✅ `fa-circle-xmark` - Error
- ✅ `fa-folder` - Folders
- ✅ `fa-clock` - Timestamp
- ✅ `fa-server` - Server
- ✅ `fa-circle-exclamation` - Alert
- ✅ `fa-note-sticky` - Notes
- ✅ `fa-cloud-arrow-up` - Upload
- ✅ `fa-house` - Home
- ✅ `fa-user` - User
- ✅ `fa-spinner` - Loading
- ✅ `fa-network-wired` - Network
- ✅ `fa-circle-user` - Profile
- ✅ `fa-location-dot` - Location
- ✅ `fa-right-to-bracket` - Login
- ✅ `fa-user-plus` - Register
- ✅ `fa-circle-info` - Information
- ✅ `fa-key` - Authentication
- ✅ `fa-circle-question` - Help
- ✅ `fa-envelope` - Email
- ✅ `fa-file-lines` - Documents
- ✅ `fa-circle-arrow-right` - Transfer
- ✅ `fa-shield-halved` - Security
- ✅ `fa-check` - Checkmark

### Brands Style Icons (fa-brands)
- ✅ `fa-microsoft` - Microsoft logo

**All icons are FREE!** No Pro subscription needed.

---

## File Size

Font Awesome Free download is approximately:
- **CSS:** ~100 KB (minified)
- **Fonts:** ~500 KB total (woff2 format)
- **Total:** ~600 KB

Much smaller than Font Awesome Pro and includes everything we need!

---

## Migration from Font Awesome Pro

If you previously had Font Awesome Pro installed:

1. **Delete old Pro files:**
   ```bash
   rm -rf wwwroot/lib/fontawesome/
   ```

2. **Download Font Awesome Free** (see Step 1 above)

3. **Copy Free files** (see Step 2 above)

4. **Test** that all icons still display correctly

All icon names are the same, so no code changes needed (already done)!

---

## License Information

Font Awesome Free is licensed under multiple open source licenses:

| Component | License | Can Commit to Git? |
|-----------|---------|-------------------|
| **Fonts (webfonts/)** | SIL OFL 1.1 | ✅ Yes |
| **Icons (SVGs)** | CC BY 4.0 | ✅ Yes |
| **CSS/Code** | MIT | ✅ Yes |

**✅ You CAN commit these files to git**  
**✅ You CAN distribute them freely**  
**✅ Perfect for open source and commercial projects**

See: https://fontawesome.com/license/free

---

## Troubleshooting

### Icons not showing (shows squares/boxes)

**Cause:** Font files not found or incorrect path

**Solution:**
1. Verify files exist in `wwwroot/lib/fontawesome/webfonts/`
2. Check `_Layout.cshtml` has correct stylesheet path
3. Clear browser cache (Ctrl+F5)
4. Check browser console for 404 errors

### Wrong icons displaying

**Cause:** Using Pro icon names that don't exist in Free

**Solution:** All icons have been updated to Free-compatible versions. Rebuild and test.

### Icons too small/large

**Solution:** Use Font Awesome sizing classes:
```html
<i class="fa-solid fa-check fa-2x"></i>  <!-- 2x size -->
<i class="fa-solid fa-check fa-lg"></i>  <!-- Large -->
<i class="fa-solid fa-check fa-xs"></i>  <!-- Extra small -->
```

---

## Additional Resources

- **Font Awesome Free Icons:** https://fontawesome.com/search?o=r&m=free
- **Documentation:** https://docs.fontawesome.com/
- **Download:** https://fontawesome.com/download
- **License:** https://fontawesome.com/license/free

---

**Need Help?**

All 2,000+ Font Awesome Free icons can be searched here:
https://fontawesome.com/search?o=r&m=free

Just filter by "Free" to see what's available!
