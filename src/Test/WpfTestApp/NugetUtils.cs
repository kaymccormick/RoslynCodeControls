using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace WpfTestApp
{
    public static class NugetUtils
    {
        public static async Task<List<string>> SaveAsync(string packageId, NuGetVersion packageVersion)
        {
            var logger0 = NullLogger.Instance;
            var cancellationToken = CancellationToken.None;

            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
            var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

            // await using var packageStream = new MemoryStream();
            
            var zipFile = @"C:\temp\ppkg.zip";
            using (var s = new FileStream(zipFile, FileMode.Create))
            {
                await resource.CopyNupkgToStreamAsync(
                    packageId,
                    packageVersion,
                    s,
                    cache,
                    logger0,
                    cancellationToken);
                s.Close();
            }

            string destinationDirectoryName = @"c:\temp\out.dir";
            Directory.Delete(destinationDirectoryName,true);
            ZipFile.ExtractToDirectory(zipFile, destinationDirectoryName);
            Debug.WriteLine($"Downloaded package {packageId} {packageVersion}");
            List<string> dlls = new List<string>();
            foreach (var enumerateFile in Directory.EnumerateFiles(destinationDirectoryName, "*.dll", SearchOption.AllDirectories))
            {
                var a = Assembly.LoadFrom(enumerateFile);
                var az = a.ExportedTypes.Where(t => typeof(DiagnosticAnalyzer).IsAssignableFrom(t));
                foreach (var type in az)
                {
                    Debug.WriteLine(type);
                }

                if (az.Any())
                {
                    dlls.Add(enumerateFile);
                }
            }

            return dlls;
        }
    }
}