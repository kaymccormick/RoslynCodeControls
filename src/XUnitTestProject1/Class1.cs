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
using Microsoft.CodeAnalysis.Diagnostics;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using WpfTestApp;
using Xunit;
using ILogger = Microsoft.Build.Framework.ILogger;

namespace XUnitTestProject1
{
    public class Class1
    {
        [Fact]
        public void Test1()
        {
            NugetUtils.SaveAsync("xunit.analyzers", new NuGetVersion("0.10.0")).Wait();
        }
    }
}