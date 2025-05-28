using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MAAS.Common.EulaVerification
{
    public class EulaDialogWindow : Window
    {
        private Button _acceptButton;
        private Button _rejectButton;
        private TextBlock _projectInfoBlock;
        private Image _qrCodeImage;
        private TextBlock _licenseBlock;

        public string ProjectName { get; set; }
        public string ProjectVersion { get; set; }
        public string LicenseUrl { get; set; }
        public BitmapImage QrCodeImage { get; set; }

        public EulaDialogWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Width = 550;
            Height = 500;
            Title = "License Agreement";
            
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Grid mainGrid = new Grid();
            Content = mainGrid;

            RowDefinition headerRow = new RowDefinition { Height = GridLength.Auto };
            RowDefinition contentRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
            RowDefinition qrCodeRow = new RowDefinition { Height = GridLength.Auto };
            RowDefinition buttonRow = new RowDefinition { Height = GridLength.Auto };

            mainGrid.RowDefinitions.Add(headerRow);
            mainGrid.RowDefinitions.Add(contentRow);
            mainGrid.RowDefinitions.Add(qrCodeRow);
            mainGrid.RowDefinitions.Add(buttonRow);

            // Header
            _projectInfoBlock = new TextBlock
            {
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(_projectInfoBlock, 0);
            mainGrid.Children.Add(_projectInfoBlock);

            // License content
            ScrollViewer scrollViewer = new ScrollViewer
            {
                Margin = new Thickness(10),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            _licenseBlock = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5)
            };
            scrollViewer.Content = _licenseBlock;

            // QR Code
            _qrCodeImage = new Image
            {
                Width = 150,
                Height = 150,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(_qrCodeImage, 2);
            mainGrid.Children.Add(_qrCodeImage);

            // Buttons
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            Grid.SetRow(buttonPanel, 3);
            mainGrid.Children.Add(buttonPanel);

            _acceptButton = new Button
            {
                Content = "Accept",
                Width = 80,
                Margin = new Thickness(5),
                IsDefault = true
            };
            _acceptButton.Click += (s, e) => { DialogResult = true; Close(); };
            buttonPanel.Children.Add(_acceptButton);

            _rejectButton = new Button
            {
                Content = "Reject",
                Width = 80,
                Margin = new Thickness(5),
                IsCancel = true
            };
            _rejectButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(_rejectButton);

            Loaded += EulaDialog_Loaded;
        }

        private void EulaDialog_Loaded(object sender, RoutedEventArgs e)
        {
            _projectInfoBlock.Text = $"{ProjectName} v{ProjectVersion}\r\nPlease read and accept the license agreement to continue:";
            
            if (QrCodeImage != null)
            {
                _qrCodeImage.Source = QrCodeImage;
            }
            else
            {
                _qrCodeImage.Visibility = Visibility.Collapsed;
            }

            _licenseBlock.Text = "By using this software, you agree to the following terms:\r\n\r\n"
                + "1. This software is provided \"as is\" without warranty of any kind.\r\n"
                + "2. This software is intended for research purposes only.\r\n"
                + "3. The creators are not liable for any damages arising from the use of this software.\r\n"
                + "4. You may not distribute or modify this software without permission.\r\n\r\n"
                + $"For the full license agreement, please visit: {LicenseUrl}";

            // Add hyperlink behavior
            if (!string.IsNullOrEmpty(LicenseUrl))
            {
                TextBlock urlBlock = new TextBlock
                {
                    Margin = new Thickness(10, 0, 10, 10),
                    TextWrapping = TextWrapping.Wrap,
                    TextDecorations = System.Windows.TextDecorations.Underline,
                    Foreground = System.Windows.Media.Brushes.Blue,
                    Text = LicenseUrl,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                urlBlock.MouseDown += (s, ev) =>
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(LicenseUrl) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error opening URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };
                Grid.SetRow(urlBlock, 2);
                Grid mainGrid = (Grid)Content;
                mainGrid.Children.Add(urlBlock);

                if (QrCodeImage != null)
                {
                    Grid.SetRow(urlBlock, 3);
                    Grid.SetRow(_qrCodeImage, 2);
                    Grid.SetRow(mainGrid.Children[3], 4); // Move button panel down
                }
                else
                {
                    Grid.SetRow(urlBlock, 2);
                    Grid.SetRow(mainGrid.Children[3], 3); // Keep button panel position
                }
            }
        }
    }
} 