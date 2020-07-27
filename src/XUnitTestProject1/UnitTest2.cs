using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;
using Xunit;

namespace XUnitTestProject1
{
    public class UnitTest2
    {
        private JoinableTaskFactory _joinableTaskFactory;

        public UnitTest2()
        {
        }

        [Fact]
        public void TestWalkerr1()
        {
            var document = TestHelper.SetupDocument(@"C:\Users\mccor.LAPTOP-T6T0BN1K\source\repos\KayMcCormick.Dev\src\RoslynCodeControls\src\RoslynCodeControls\LineInfo2.cs", Host);
            SyntaxTree tree = null;
            JoinableTaskFactory.Run(async () =>
            {
                tree = await GetDocumentSyntaxTreeAsync(document);
            });
            var walker = new Walker();
            walker.Visit(tree.GetRoot());
            DumpNodes(walker.CompilationUnitNode);


        }

        private void DumpNodes(StructureNode node, int depth = 0)
        {

            Debug.WriteLine($"{depth:D2} " + String.Join("", Enumerable.Repeat("  ", depth)) + 
                node.DisplayText);
            foreach (var structureNode in node.Children)
            {
                DumpNodes(structureNode, depth + 1);
            }
        }

        private async Task<SyntaxTree> GetDocumentSyntaxTreeAsync(Document document)
        {
            var tree = await document.GetSyntaxTreeAsync();
            return tree;
        }

        [WpfFact]
        public void TestClassDiagram1()
        {
            var document = TestHelper.SetupDocument(@"C:\temp\commontext.cs", Host);

            var w = new Window();
            var diagram = new ClassDiagram {Document = document};
            var grid = new Grid();
            grid.Children.Add(diagram);
            w.Content = grid;
            w.ShowDialog();
        }

        private JoinableTaskFactory JoinableTaskFactory 
        {
            get { return _joinableTaskFactory ?? (_joinableTaskFactory = new JoinableTaskFactory(new JoinableTaskContext())); }
        }

        private static HostServices Host { get; set; }
    }
}