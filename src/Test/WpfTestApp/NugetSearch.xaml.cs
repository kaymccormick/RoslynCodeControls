using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Accessibility;
using Microsoft.CodeAnalysis.Diagnostics;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Path = System.IO.Path;

namespace WpfTestApp
{
    /// <summary>
    /// Interaction logic for NugetSearch.xaml
    /// </summary>
    public partial class NugetSearch : UserControl
    {
        public NugetSearch()
        {
            InitializeComponent();
        }

        public string AnalyzersDir { get; set; }

        public async Task SearchAsync(string searchTerm)
        {
            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            PackageSearchResource resource = await repository.GetResourceAsync<PackageSearchResource>();
            SearchFilter searchFilter = new SearchFilter(includePrerelease: true);

            IEnumerable<IPackageSearchMetadata> results = await resource.SearchAsync(
                searchTerm,
                searchFilter,
                skip: 0,
                take: 20,
                logger,
                cancellationToken);
            Results.ItemsSource = results;
            // foreach (IPackageSearchMetadata result in results)
            // {
                // Debug.WriteLine($"Found package {result.Identity.Id} {result.Identity.Version}");
            // }
        }

        private async void DoSearch(object sender, RoutedEventArgs e)
        {
            await SearchAsync(SearchTerm.Text);
        }

        private async void SaveAsync(object sender, ExecutedRoutedEventArgs e)
        {
            var p = (PackageSearchMetadataBuilder.ClonedPackageSearchMetadata) e.Parameter;

            var packageId = p.Identity.Id;
            
            var packageVersion = p.Identity.Version;
            var dlls  = await NugetUtils.SaveAsync(packageId, packageVersion);
            foreach (var dll in dlls)
            {
                var destFile = Path.Combine(AnalyzersDir, Path.GetFileName(dll));
                File.Copy(dll, destFile);
                SavedFiles.Add(destFile);
            }
            Window.GetWindow(this)?.Close();

        }

        public List<string> SavedFiles { get; set; } = new List<string>();
    }
}
