using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Windows;
using System.Windows.Input;

namespace ZLFileRelay.ConfigTool.Views;

public partial class ActiveDirectoryGroupBrowser : Window
{
    private List<SecurityGroupInfo> _allGroups = new();
    public string? SelectedGroupName { get; private set; }

    public ActiveDirectoryGroupBrowser()
    {
        InitializeComponent();
        LoadAllGroups();
    }

    private void LoadAllGroups()
    {
        try
        {
            _allGroups.Clear();

            // Query Windows Security Groups
            var searcher = new ManagementObjectSearcher(
                "SELECT Name, LocalAccount, SID FROM Win32_Group WHERE LocalAccount=True");
            
            foreach (ManagementObject obj in searcher.Get())
            {
                var groupName = obj["Name"]?.ToString() ?? "";
                var sid = obj["SID"]?.ToString() ?? "";
                var localAccount = obj["LocalAccount"] != null && (bool)obj["LocalAccount"];

                if (!string.IsNullOrWhiteSpace(groupName))
                {
                    _allGroups.Add(new SecurityGroupInfo
                    {
                        Name = groupName,
                        Domain = Environment.MachineName,
                        FullName = $"{Environment.MachineName}\\{groupName}",
                        IsLocal = localAccount
                    });
                }
            }

            // Also query domain groups if on a domain
            try
            {
                var domainGroups = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_Group WHERE LocalAccount=False");
                
                foreach (ManagementObject obj in domainGroups.Get())
                {
                    var groupName = obj["Name"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(groupName))
                    {
                        // Try to get domain name
                        var domainName = Environment.UserDomainName;
                        _allGroups.Add(new SecurityGroupInfo
                        {
                            Name = groupName,
                            Domain = domainName,
                            FullName = $"{domainName}\\{groupName}",
                            IsLocal = false
                        });
                    }
                }
            }
            catch
            {
                // Not on domain or don't have permission, that's okay
            }

            // Sort and populate
            _allGroups = _allGroups.OrderBy(g => g.Domain).ThenBy(g => g.Name).ToList();
            GroupsListBox.ItemsSource = _allGroups;
            GroupsListBox.DisplayMemberPath = "FullName";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading security groups: {ex.Message}\n\n" +
                          "Note: Local groups are loaded. Domain groups may require additional permissions.",
                          "Load Error",
                          MessageBoxButton.OK,
                          MessageBoxImage.Warning);
        }
    }

    private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SearchButton_Click(sender, e);
        }
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var searchText = SearchTextBox.Text?.ToLower() ?? "";
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            GroupsListBox.ItemsSource = _allGroups;
            GroupsListBox.DisplayMemberPath = "FullName";
        }
        else
        {
            var filtered = _allGroups
                .Where(g => g.Name.ToLower().Contains(searchText) || 
                           g.Domain.ToLower().Contains(searchText) ||
                           g.FullName.ToLower().Contains(searchText))
                .ToList();
            GroupsListBox.ItemsSource = filtered;
            GroupsListBox.DisplayMemberPath = "FullName";
        }
    }

    private void GroupsListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (GroupsListBox.SelectedItem is SecurityGroupInfo selected)
        {
            SelectedGroupText.Text = $"Selected: {selected.FullName}";
            AddButton.IsEnabled = true;
            SelectedGroupName = selected.FullName;
        }
        else
        {
            SelectedGroupText.Text = "No group selected";
            AddButton.IsEnabled = false;
            SelectedGroupName = null;
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedGroupName = null;
        DialogResult = false;
        Close();
    }
}

internal class SecurityGroupInfo
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsLocal { get; set; }
}

