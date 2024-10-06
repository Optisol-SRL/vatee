using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Vatee.ApiPull;

namespace Vatee
{
    public partial class TabApiPull : VateeControl
    {
        private readonly Dictionary<string, X509Certificate2> _certificatesByThumbprint = new();

        public TabApiPull()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (_certificatesByThumbprint.Count == 0)
            {
                LoadCertificates();
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            var inputId = IdTextBox.Text;

            if (string.IsNullOrWhiteSpace(inputId) || CertificateComboBox.SelectedItem == null)
            {
                IdStatusTextBlock.Text = "Alege un certificat și CUI-ul firmei pentru care vrei să cerem detaliile.";
                return;
            }

            var selectedCert = CertificateComboBox.SelectedItem as CertificateDisplayModel;
            if (selectedCert == null || !_certificatesByThumbprint.TryGetValue(selectedCert.Thumbprint, out var selectedCertificate))
            {
                await ShowMessageDialog("Nu am putut determina certificatul folosit pentru autentificare.");
                return;
            }

            var fiscalId = 40379753;
            var refDate = new DateOnly(2024, 7, 1);

            var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Vatee", "data");
            Directory.CreateDirectory(dataPath);

            var projectedFilePath = Path.Combine(dataPath, $"data_{fiscalId}_{refDate:MMyyyy}.zip");
            var doNewPull = true;
            if (File.Exists(projectedFilePath))
            {
                doNewPull = await ShowYesNoDialog("Am găsit un rezultat deja descărcat pentru această perioadă. Vrei să-l folosești?");
            }

            var vatSheetGetter = new GetVatSheet();
            GetVatSheet.Response result;
            try
            {
                result = await vatSheetGetter.Get(fiscalId, refDate, selectedCertificate);
                if (result.ResultType == VatSheetCheckResult.Maintenance)
                {
                    await ShowMessageDialog("Mentenanta ANAF");
                }
            }
            catch (Exception exception) when (exception.Message.Contains("The SSL connection could not be established"))
            {
                await ShowMessageDialog(
                    "Nu am putut folosi certificatul pentru autentificare. Asigură-te că stick-ul este în calculator și ai introdus parola corectă."
                );
                return;
            }

            if (result.BodyString != null) { }

            if (result.ErrorMessage != null)
            {
                Console.WriteLine(result.ErrorMessage.Error);
            }

            ApiProgressBar.IsVisible = true;
            StartButton.IsEnabled = false;

            ApiProgressBar.IsVisible = false;
            StartButton.IsEnabled = true;
        }

        private void LoadCertificates()
        {
            try
            {
                var certificates = GetValidCertificates();
                var models = new List<CertificateDisplayModel>();

                foreach (var cert in certificates)
                {
                    // ReSharper disable once InconsistentNaming
                    string getCN(string full)
                    {
                        var parts = full.Split(',');
                        var cnPart = parts.Where(r => r.Trim().StartsWith("CN=")).FirstOrDefault();
                        var name = cnPart?[3..] ?? cert.Subject;
                        return name;
                    }

                    models.Add(
                        new CertificateDisplayModel
                        {
                            Thumbprint = cert.Thumbprint,
                            DisplayName =
                                $"{getCN(cert.Subject)}\n {getCN(cert.IssuerName.Name)}\n Expiră: {cert.GetExpirationDateString()})",
                        }
                    );

                    _certificatesByThumbprint[cert.Thumbprint] = cert;
                }

                CertificateComboBox.ItemsSource = models;
                CertificateComboBox.DisplayMemberBinding = new Binding("DisplayName");

                // Hide loading text and enable the ComboBox
                LoadingCertificatesText.IsVisible = false;
                CertificateComboBox.IsEnabled = true;
            }
            catch (Exception ex)
            {
                //TODO: show an inline message
            }
        }

        private List<X509Certificate2> GetValidCertificates()
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            var validCerts = store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

            // see https://github.com/lucianMDL/OAuth2-Anaf/blob/409874cb544207cd2bdaa5fe92e61c7dbb14d79e/OAuth2-Anaf/Form1.cs#L129C403-L129C412
            var allowedIssuers = new List<string> { "CERTSIGN", "DIGISIGN", "TRAN", "ALFASIGN", "CERT DIGI", "CERTDIGITAL", "DE CALCUL" };
            var certificates = validCerts
                .Where(r => allowedIssuers.Any(x => r.IssuerName.Name.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                .ToList();

            store.Close();
            return certificates;
        }

        private void IdTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var inputId = IdTextBox.Text;

            StartButton.IsEnabled = CertificateComboBox.SelectionBoxItem != null;

            //TODO: validation
        }
    }

    public class CertificateDisplayModel
    {
        public string Thumbprint { get; set; }
        public string DisplayName { get; set; }
    }
}
