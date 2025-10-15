# Branding Assets

This folder contains branding assets for the ZL File Relay Config Tool.

## Current Assets

- **ZLLogo.png** - Main logo displayed in the About tab

## Adding Custom ZentrixLabs Branding

### Option 1: Custom Font

If you have a custom font for "ZENTRIXLABS":

1. **Add Font File:**
   - Place font file here: `Assets/YourFont.ttf` or `Assets/YourFont.otf`

2. **Update .csproj:**
   ```xml
   <ItemGroup>
     <Resource Include="Assets\YourFont.ttf" />
   </ItemGroup>
   ```

3. **Update AboutView.xaml:**
   ```xaml
   <TextBlock Text="ZENTRIXLABS" 
              FontSize="32" 
              FontWeight="Light"
              LetterSpacing="4"
              Foreground="White"
              FontFamily="/Assets/#YourFontName">
       <!-- Replace #YourFontName with the actual font family name -->
   </TextBlock>
   ```

### Option 2: SVG Logo

If you have an SVG version of the ZentrixLabs logo:

1. **Convert SVG to XAML:**
   - Use a tool like Inkscape or https://github.com/BerndK/SvgToXaml
   - Export as XAML path data

2. **Add to Assets:**
   - Save as `Assets/ZentrixLabsLogo.xaml` as a ResourceDictionary

3. **Update AboutView.xaml:**
   ```xaml
   <UserControl.Resources>
       <ResourceDictionary Source="/Assets/ZentrixLabsLogo.xaml"/>
   </ResourceDictionary>
   
   <!-- In your logo section, replace TextBlock with: -->
   <Viewbox Width="250" Height="80">
       <Path Data="{StaticResource ZentrixLabsLogoPath}" 
             Fill="White"
             Stretch="Uniform"/>
   </Viewbox>
   ```

### Option 3: PNG/Image Logo

If you have a PNG/image for ZentrixLabs:

1. **Add Image:**
   - Place file here: `Assets/ZentrixLabsLogo.png`

2. **Update .csproj:**
   ```xml
   <ItemGroup>
     <Resource Include="Assets\ZentrixLabsLogo.png" />
   </ItemGroup>
   ```

3. **Update AboutView.xaml:**
   ```xaml
   <!-- Replace the TextBlock with: -->
   <Image Source="/Assets/ZentrixLabsLogo.png" 
          Width="250" 
          Stretch="Uniform"
          Margin="0,0,0,10"/>
   ```

## Current Placeholder

The About tab currently displays "ZENTRIXLABS" as styled text with:
- Font Size: 32
- Letter Spacing: 4
- Color: White
- Drop Shadow Effect
- Charcoal grey background (#FF3C3C3C)

Replace this placeholder with your custom branding when ready.

## Recommended Specifications

### Logo Image
- **Format:** PNG with transparency
- **Size:** 800x800px or larger (square aspect ratio)
- **Background:** Transparent
- **Color Mode:** RGB

### Brand Text Logo
- **Format:** PNG with transparency or SVG
- **Width:** 800-1000px
- **Height:** 200-300px
- **Background:** Transparent
- **Optimized for:** White/light text on dark background

### Custom Font
- **Format:** TTF or OTF
- **Licensing:** Ensure you have rights to embed in applications
- **File Size:** Preferably <500KB

## Links to Update

When updating branding, also update these placeholders in AboutView.xaml:

1. **GitHub Link:**
   - Current: `https://github.com/zentrixlabs/zlfilerelay`
   - Update to your actual repository URL

2. **Website Link:**
   - Current: `https://zentrixlabs.com`
   - Update to your actual website

3. **Support Email:**
   - Current: `support@zentrixlabs.com`
   - Update to your actual support email

## Example: Complete Custom Branding

```xaml
<!-- Custom Logo Section -->
<Border Background="#FF3C3C3C" 
        CornerRadius="8" 
        Padding="40"
        Margin="0,0,0,30"
        BorderBrush="#FF555555"
        BorderThickness="1">
    <StackPanel HorizontalAlignment="Center">
        <!-- ZL Logo -->
        <Image Source="/Assets/ZLLogo.png" 
               Width="220" 
               Height="220"
               Stretch="Uniform"
               Margin="0,0,0,20"/>
        
        <!-- ZentrixLabs Custom Logo/Font -->
        <Image Source="/Assets/ZentrixLabsLogo.png" 
               Width="300" 
               Stretch="Uniform"
               Margin="0,0,0,5"/>
        
        <TextBlock Text="File Transfer Solutions" 
                   FontSize="14" 
                   FontStyle="Italic"
                   Foreground="#FFB0B0B0"
                   HorizontalAlignment="Center"/>
    </StackPanel>
</Border>
```

---

**Last Updated:** October 8, 2025  
**Status:** ✅ Logo in place, custom branding placeholders ready
