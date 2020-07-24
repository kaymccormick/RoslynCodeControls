using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation;
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

        public async Task Search(string searchTerm)
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

        private void DoSearch(object sender, RoutedEventArgs e)
        {
            Search(SearchTerm.Text);
        }

        private async void Save(object sender, ExecutedRoutedEventArgs e)
        {
            var p = (PackageSearchMetadataBuilder.ClonedPackageSearchMetadata) e.Parameter;

            ILogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            SourceCacheContext cache = new SourceCacheContext();
            SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            string packageId = p.Identity.Id;
            
            NuGetVersion packageVersion = p.Identity.Version;
            await using MemoryStream packageStream = new MemoryStream();

            await resource.CopyNupkgToStreamAsync(
                packageId,
                packageVersion,
                packageStream,
                cache,
                logger,
                cancellationToken);

            Debug.WriteLine($"Downloaded package {packageId} {packageVersion}");

            using PackageArchiveReader packageReader = new PackageArchiveReader(packageStream);
            NuspecReader nuspecReader = await packageReader.GetNuspecReaderAsync(cancellationToken);
            foreach (var file in packageReader.GetFiles().Where(f=>Path.GetExtension(f).ToLowerInvariant()==".dll"))
            {
                var s = packageReader.GetStream(file);
                var tf = Path.GetTempFileName();
                packageReader.ExtractFile(file, tf, logger);
                try
                {
                    var a = Assembly.LoadFrom(tf);
                    var az = a.ExportedTypes.Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t));
                    foreach (var type in az)
                    {
                        Debug.WriteLine(type);
                    }
                }
                catch
                {

                }
            }
            Debug.WriteLine($"Tags: {nuspecReader.GetTags()}");
            Debug.WriteLine($"Description: {nuspecReader.GetDescription()}");
        }
    }
}
