﻿using System.Windows.Controls;

namespace NuGet.Client.VisualStudio.UI
{
    /// <summary>
    /// Interaction logic for PackageMetadata.xaml
    /// </summary>
    public partial class PackageMetadataControl : UserControl
    {
        public PackageMetadataControl()
        {
            InitializeComponent();

            Visibility = System.Windows.Visibility.Collapsed;
            this.DataContextChanged += PackageMetadataControl_DataContextChanged;
        }

        void PackageMetadataControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is UiPackageMetadata)
            {
                Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                Visibility = System.Windows.Visibility.Collapsed;
            }
        }
    }
}
