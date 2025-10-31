using FontAwesome.Sharp;

namespace ZLFileRelay.ConfigTool.Resources;

/// <summary>
/// Provides centralized icon definitions using Font Awesome icons via FontAwesome.Sharp.
/// Reference: https://fontawesome.com/icons
/// </summary>
public static class IconResources
{
    // Service Management Icons
    public static IconChar Refresh => IconChar.ArrowsRotate;        // Refresh/reload
    public static IconChar Download => IconChar.Download;            // Download/install
    public static IconChar Delete => IconChar.TrashCan;              // Delete/uninstall
    public static IconChar Play => IconChar.Play;                    // Start service
    public static IconChar Stop => IconChar.Stop;                    // Stop service
    public static IconChar StatusCircle => IconChar.Circle;          // Status indicator
    public static IconChar Sync => IconChar.ArrowsRotate;            // Sync

    // Configuration Icons
    public static IconChar Permissions => IconChar.UserShield;       // Permissions/security
    public static IconChar Save => IconChar.FloppyDisk;              // Save configuration
    public static IconChar Settings => IconChar.Gear;                // Settings
    public static IconChar Folder => IconChar.Folder;                // Folder
    public static IconChar FolderOpen => IconChar.FolderOpen;        // Browse folders
    public static IconChar Document => IconChar.File;                // Document/file

    // SSH & Security Icons
    public static IconChar Key => IconChar.Key;                      // SSH keys
    public static IconChar View => IconChar.Eye;                     // View/preview
    public static IconChar Copy => IconChar.Copy;                    // Copy to clipboard
    public static IconChar TestBeaker => IconChar.Flask;             // Test/validate
    public static IconChar Shield => IconChar.Shield;                // Security/protection
    public static IconChar Certificate => IconChar.Certificate;      // SSL certificates

    // Network & Connection Icons
    public static IconChar Globe => IconChar.Globe;                  // Web/internet
    public static IconChar CloudUpload => IconChar.CloudArrowUp;     // Upload to cloud
    public static IconChar Server => IconChar.Server;                // Server
    public static IconChar Remote => IconChar.NetworkWired;          // Remote connection
    public static IconChar Connect => IconChar.Plug;                 // Connect
    public static IconChar Disconnect => IconChar.PlugCircleExclamation; // Disconnect (Free alternative)

    // User & Account Icons
    public static IconChar Contact => IconChar.User;                 // User/contact
    public static IconChar Admin => IconChar.UserTie;                // Administrator
    public static IconChar Lock => IconChar.Lock;                    // Locked/secure
    public static IconChar Unlock => IconChar.LockOpen;              // Unlocked

    // Status & Feedback Icons
    public static IconChar CheckMark => IconChar.Check;              // Success/valid
    public static IconChar Warning => IconChar.TriangleExclamation;  // Warning
    public static IconChar Error => IconChar.CircleXmark;            // Error
    public static IconChar Info => IconChar.CircleInfo;              // Information
    public static IconChar StatusCircleCheckmark => IconChar.CircleCheck; // Active/success

    // Action Icons
    public static IconChar Repair => IconChar.Wrench;                // Repair/fix
    public static IconChar Clear => IconChar.Eraser;                 // Clear
    public static IconChar Export => IconChar.FileExport;            // Export data
    public static IconChar Import => IconChar.FileImport;            // Import data
    public static IconChar Add => IconChar.Plus;                     // Add new
    public static IconChar Remove => IconChar.Minus;                 // Remove
    public static IconChar Edit => IconChar.PenToSquare;             // Edit

    // Navigation Icons
    public static IconChar Home => IconChar.House;                   // Home/dashboard
    public static IconChar ChevronRight => IconChar.ChevronRight;    // Navigate forward
    public static IconChar ChevronDown => IconChar.ChevronDown;      // Expand/collapse
    public static IconChar More => IconChar.EllipsisVertical;        // More options

    // Log & Output Icons
    public static IconChar List => IconChar.ListUl;                  // List view
    public static IconChar Dictionary => IconChar.Book;              // Dictionary/logs
    public static IconChar Search => IconChar.MagnifyingGlass;       // Search

    // Additional useful icons
    public static IconChar Network => IconChar.NetworkWired;         // Network topology
    public static IconChar Upload => IconChar.Upload;                // Upload files
    public static IconChar Database => IconChar.Database;            // Database
    public static IconChar History => IconChar.ClockRotateLeft;     // History/recent
    public static IconChar Terminal => IconChar.Terminal;            // Command line
    public static IconChar Code => IconChar.Code;                    // Code/scripts
}