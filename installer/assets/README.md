# Installer Assets

This directory contains branding assets for the Inno Setup installer.

## Current Assets

### ✅ Available
- **icon.ico** - Installer executable icon (ZLFileRelay app icon)
- **ZLFileRelay.png** - Application icon (PNG format)
- **ZLLogo.png** - Company logo (reference)

### 🔧 To Be Created
For a more professional installer appearance, create these optional branding images:

1. **WizardImage.bmp**
   - Size: 164 x 314 pixels
   - Usage: Large banner on left side of installer wizard
   - Format: 24-bit BMP
   - Recommendation: Use company branding, product logo, or themed image

2. **WizardSmallImage.bmp**
   - Size: 55 x 58 pixels  
   - Usage: Small icon in top-right of installer wizard pages
   - Format: 24-bit BMP
   - Recommendation: Simplified version of product logo

## How to Add Branded Images

### Option 1: Professional Design
1. Create images with your design tool (Photoshop, GIMP, etc.)
2. Save as 24-bit BMP files
3. Place in this directory
4. Uncomment lines in `ZLFileRelay.iss`:
   ```iss
   WizardImageFile=installer\assets\WizardImage.bmp
   WizardSmallImageFile=installer\assets\WizardSmallImage.bmp
   ```

### Option 2: Convert Existing Logo
Using ZLLogo.png as a starting point:

**PowerShell (requires ImageMagick):**
```powershell
# Install ImageMagick if needed: winget install ImageMagick.ImageMagick

# Create large wizard image (164x314)
magick convert ZLLogo.png -resize 164x314^ -gravity center -extent 164x314 WizardImage.bmp

# Create small wizard image (55x58)
magick convert ZLLogo.png -resize 55x58^ -gravity center -extent 55x58 WizardSmallImage.bmp
```

**Using GIMP (free tool):**
1. Open ZLLogo.png
2. Scale to target size (Image → Scale Image)
3. Export as BMP (File → Export As → select BMP)

### Option 3: Use Defaults (Current)
Inno Setup will use default graphics until custom images are provided. This works fine for internal/testing deployments.

## Testing

After adding custom images:
1. Rebuild installer: `iscc installer\ZLFileRelay.iss`
2. Run installer and verify images appear correctly
3. Check image quality and branding consistency

## Design Tips

### Large Wizard Image (164x314)
- Use vertical composition
- Keep important elements away from edges
- Consider: product screenshot, logo with tagline, themed graphic
- Test with both light and dark system themes

### Small Wizard Image (55x58)
- Simple, recognizable icon or logo
- High contrast for visibility
- Avoid fine details (will be too small)

## Current Status

✅ **Installer is functional** - icon.ico provides branded installer executable  
⏳ **Wizard images optional** - Using Inno Setup defaults (clean, professional)  
🎨 **Enhancement opportunity** - Add custom wizard images for branded experience  

## Example Workflow

For a fully branded installer:
1. Design or adapt existing brand assets
2. Create 164x314 and 55x58 images
3. Convert to 24-bit BMP format
4. Place in this directory
5. Update ZLFileRelay.iss to uncomment wizard image lines
6. Rebuild installer
7. Test appearance

---

**Current State**: Minimal viable branding (installer icon only)  
**Future State**: Full branded installer experience with custom wizard images

