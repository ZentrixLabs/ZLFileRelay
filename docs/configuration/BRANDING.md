# ZL File Relay - Branding Guide

## Logo Usage

The ZL logo (`ZLLogo.png`) is now available in both the Web Portal and Config Tool.

---

## Web Portal Usage

**Location:** `wwwroot/images/ZLLogo.png`

### In Razor Pages/Views

```html
<!-- Simple image -->
<img src="~/images/ZLLogo.png" alt="ZL File Relay Logo" />

<!-- With sizing -->
<img src="~/images/ZLLogo.png" alt="ZL File Relay Logo" width="200" />

<!-- Responsive image -->
<img src="~/images/ZLLogo.png" alt="ZL File Relay Logo" class="img-fluid" style="max-width: 200px;" />

<!-- In navbar brand -->
<a class="navbar-brand" asp-page="/Index">
    <img src="~/images/ZLLogo.png" alt="ZL File Relay" height="32" class="d-inline-block align-text-top me-2" />
    @Config.Branding.ProductName
</a>
```

### About Page Example

```html
@page
@{
    ViewData["Title"] = "About";
}

<div class="container mt-5">
    <div class="text-center mb-4">
        <img src="~/images/ZLLogo.png" alt="ZL File Relay Logo" style="max-width: 300px;" class="img-fluid mb-3" />
        <h1>About ZL File Relay</h1>
        <p class="lead">Enterprise File Transfer Solution</p>
    </div>
    
    <div class="row">
        <div class="col-md-8 mx-auto">
            <div class="card">
                <div class="card-body">
                    <h3>Version Information</h3>
                    <p><strong>Version:</strong> 1.0.0</p>
                    <p><strong>Company:</strong> @Config.Branding.CompanyName</p>
                    <p><strong>Support:</strong> <a href="mailto:@Config.Branding.SupportEmail">@Config.Branding.SupportEmail</a></p>
                </div>
            </div>
        </div>
    </div>
</div>
```

---

## WPF Config Tool Usage

**Location:** `Assets/ZLLogo.png`

### In XAML

```xaml
<!-- Simple image -->
<Image Source="/Assets/ZLLogo.png" Width="200" />

<!-- With stretch mode -->
<Image Source="/Assets/ZLLogo.png" 
       Width="200" 
       Stretch="Uniform" />

<!-- Centered in a border -->
<Border Padding="20" Background="White">
    <Image Source="/Assets/ZLLogo.png" 
           Width="250" 
           HorizontalAlignment="Center" 
           Stretch="Uniform" />
</Border>
```

### About Tab - Already Implemented! ✅

The Config Tool now has a complete About tab at `Views/AboutView.xaml` with:
- Your ZL logo in a charcoal grey box
- ZentrixLabs branding placeholder
- Version information
- Key features list
- Links to GitHub and website
- Professional ModernWPF styling

To customize the ZentrixLabs branding, see `Assets/BRANDING_README.md`.

### About Tab Example (Already Done)

```xaml
<TabItem Header="About">
    <ScrollViewer>
        <StackPanel Margin="20" MaxWidth="600" HorizontalAlignment="Center">
            
            <!-- Logo -->
            <Border Padding="20" Background="White" CornerRadius="8" 
                    BorderBrush="{DynamicResource SystemControlBackgroundBaseLowBrush}" 
                    BorderThickness="1" 
                    Margin="0,0,0,20">
                <Image Source="/Assets/ZLLogo.png" 
                       Width="250" 
                       HorizontalAlignment="Center" 
                       Stretch="Uniform" />
            </Border>
            
            <!-- Product Info -->
            <TextBlock Text="ZL File Relay" 
                       FontSize="24" 
                       FontWeight="Bold" 
                       TextAlignment="Center" 
                       Margin="0,0,0,10"/>
            
            <TextBlock Text="Enterprise File Transfer Solution" 
                       FontSize="14" 
                       TextAlignment="Center" 
                       Foreground="Gray" 
                       Margin="0,0,0,20"/>
            
            <!-- Version Info -->
            <GroupBox Header="Version Information" Margin="0,0,0,20">
                <StackPanel Margin="10">
                    <TextBlock Margin="0,0,0,10">
                        <Run Text="Version: " FontWeight="Bold"/>
                        <Run Text="1.0.0"/>
                    </TextBlock>
                    <TextBlock Margin="0,0,0,10">
                        <Run Text="Company: " FontWeight="Bold"/>
                        <Run Text="{Binding CompanyName}"/>
                    </TextBlock>
                    <TextBlock Margin="0,0,0,10">
                        <Run Text="Support: " FontWeight="Bold"/>
                        <Hyperlink NavigateUri="{Binding SupportEmailUri}">
                            <Run Text="{Binding SupportEmail}"/>
                        </Hyperlink>
                    </TextBlock>
                </StackPanel>
            </GroupBox>
            
            <!-- Features -->
            <GroupBox Header="Features">
                <StackPanel Margin="10">
                    <TextBlock Margin="0,0,0,5">
                        <Run Text="✓" Foreground="Green"/>
                        <Run Text=" Automated File Transfer (SSH/SMB)"/>
                    </TextBlock>
                    <TextBlock Margin="0,0,0,5">
                        <Run Text="✓" Foreground="Green"/>
                        <Run Text=" Web Portal with Windows Authentication"/>
                    </TextBlock>
                    <TextBlock Margin="0,0,0,5">
                        <Run Text="✓" Foreground="Green"/>
                        <Run Text=" Remote Configuration Management"/>
                    </TextBlock>
                    <TextBlock Margin="0,0,0,5">
                        <Run Text="✓" Foreground="Green"/>
                        <Run Text=" Enterprise Security (DPAPI, SSH Keys)"/>
                    </TextBlock>
                </StackPanel>
            </GroupBox>
            
        </StackPanel>
    </ScrollViewer>
</TabItem>
```

### In MainWindow Title/Header

```xaml
<Window x:Class="ZLFileRelay.ConfigTool.MainWindow"
        ...>
    
    <!-- Window Icon -->
    <Window.Icon>
        <BitmapImage UriSource="/Assets/ZLLogo.png"/>
    </Window.Icon>
    
    <!-- Header with logo -->
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>
        
        <!-- Header -->
        <Border Grid.Row="0" Background="{DynamicResource SystemControlBackgroundAccentBrush}" Padding="15">
            <StackPanel Orientation="Horizontal">
                <Image Source="/Assets/ZLLogo.png" Width="40" Height="40" Margin="0,0,15,0"/>
                <StackPanel VerticalAlignment="Center">
                    <TextBlock Text="ZL File Relay" FontSize="18" FontWeight="Bold" Foreground="White"/>
                    <TextBlock Text="Configuration Tool" FontSize="12" Foreground="WhiteSmoke"/>
                </StackPanel>
            </StackPanel>
        </Border>
        
        <!-- Main content -->
        <TabControl Grid.Row="1">
            <!-- Your tabs here -->
        </TabControl>
    </Grid>
</Window>
```

---

## Logo Specifications

**File:** `ZLLogo.png`

**Recommended Sizes:**
- **Web Portal Header:** 32-48px height
- **Web Portal About Page:** 200-300px width
- **Config Tool Window Icon:** 32x32px or 48x48px
- **Config Tool About Tab:** 200-300px width
- **Config Tool Header:** 40-50px height

**Best Practices:**
- Always include `alt` text for web images
- Use `Stretch="Uniform"` in WPF to maintain aspect ratio
- Consider dark/light themes - ensure logo works on both backgrounds
- For small sizes (icons), consider creating a simplified version

---

## Customization

To update the logo for a specific deployment:

1. Replace `ZLLogo.png` in the solution root
2. Run the copy commands:
   ```powershell
   Copy-Item "ZLLogo.png" "src\ZLFileRelay.WebPortal\wwwroot\images\ZLLogo.png"
   Copy-Item "ZLLogo.png" "src\ZLFileRelay.ConfigTool\Assets\ZLLogo.png"
   ```
3. Rebuild both projects

Alternatively, update the logo files directly in their respective locations.

---

## File Locations

```
ZLFileRelay/
├── ZLLogo.png                                    ← Source logo (master copy)
├── src/
│   ├── ZLFileRelay.WebPortal/
│   │   └── wwwroot/
│   │       └── images/
│   │           └── ZLLogo.png                   ← Web portal logo
│   └── ZLFileRelay.ConfigTool/
│       └── Assets/
│           └── ZLLogo.png                       ← Config tool logo
```

---

**Last Updated:** October 8, 2025  
**Status:** ✅ Logo configured and ready to use
