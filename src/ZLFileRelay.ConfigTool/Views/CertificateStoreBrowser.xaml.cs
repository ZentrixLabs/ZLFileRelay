using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;

namespace ZLFileRelay.ConfigTool.Views;

public partial class CertificateStoreBrowser : Window
{
    public X509Certificate2? SelectedCertificate { get; private set; }

    public CertificateStoreBrowser(List<X509Certificate2> certificates, string? currentThumbprint = null)
    {
        InitializeComponent();
        
        // Create view models for certificates with status information
        var certViewModels = certificates.Select(cert => new CertificateViewModel(cert)).ToList();
        CertificatesDataGrid.ItemsSource = certViewModels;

        // Select current certificate if provided
        if (!string.IsNullOrWhiteSpace(currentThumbprint))
        {
            var currentThumbprintClean = currentThumbprint.Replace(" ", "").Replace("-", "");
            var currentCert = certViewModels.FirstOrDefault(c => 
                c.Thumbprint.Replace(" ", "").Replace("-", "")
                    .Equals(currentThumbprintClean, StringComparison.OrdinalIgnoreCase));
            
            if (currentCert != null)
            {
                CertificatesDataGrid.SelectedItem = currentCert;
                CertificatesDataGrid.ScrollIntoView(currentCert);
            }
        }
    }

    private void CertificatesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CertificatesDataGrid.SelectedItem is CertificateViewModel certVm)
        {
            UpdateCertificateDetails(certVm.Certificate);
            SelectButton.IsEnabled = true;
        }
        else
        {
            ClearCertificateDetails();
            SelectButton.IsEnabled = false;
        }
    }

    private void UpdateCertificateDetails(X509Certificate2 cert)
    {
        SubjectTextBlock.Text = cert.Subject;
        ThumbprintTextBlock.Text = cert.Thumbprint;
        ExpiryTextBlock.Text = cert.NotAfter.ToString("yyyy-MM-dd HH:mm:ss");
        
        if (cert.NotAfter < DateTime.Now)
        {
            ExpiryTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            ExpiryTextBlock.Text += " (EXPIRED)";
        }
        else if (cert.NotAfter < DateTime.Now.AddDays(30))
        {
            ExpiryTextBlock.Foreground = System.Windows.Media.Brushes.Orange;
            ExpiryTextBlock.Text += " (Expires soon)";
        }
        else
        {
            ExpiryTextBlock.Foreground = System.Windows.Media.Brushes.Black;
        }
        
        PrivateKeyTextBlock.Text = cert.HasPrivateKey ? "Yes" : "No";
        PrivateKeyTextBlock.Foreground = cert.HasPrivateKey 
            ? System.Windows.Media.Brushes.Green 
            : System.Windows.Media.Brushes.Red;
    }

    private void ClearCertificateDetails()
    {
        SubjectTextBlock.Text = string.Empty;
        ThumbprintTextBlock.Text = string.Empty;
        ExpiryTextBlock.Text = string.Empty;
        PrivateKeyTextBlock.Text = string.Empty;
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (CertificatesDataGrid.SelectedItem is CertificateViewModel certVm)
        {
            SelectedCertificate = certVm.Certificate;
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // View model class for certificate display
    private class CertificateViewModel
    {
        public X509Certificate2 Certificate { get; }
        public string Subject => Certificate.Subject;
        public string Issuer => Certificate.Issuer;
        public string Thumbprint => Certificate.Thumbprint;
        public DateTime NotBefore => Certificate.NotBefore;
        public DateTime NotAfter => Certificate.NotAfter;
        public string StatusText
        {
            get
            {
                if (!Certificate.HasPrivateKey)
                    return "No Private Key";
                if (Certificate.NotAfter < DateTime.Now)
                    return "Expired";
                if (Certificate.NotAfter < DateTime.Now.AddDays(30))
                    return "Expires Soon";
                return "Valid";
            }
        }

        public CertificateViewModel(X509Certificate2 certificate)
        {
            Certificate = certificate;
        }
    }
}

