using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class CompilationControl : Control
    {

        static CompilationControl()
        {
            RoslynProperties.CompilationProperty.AddOwner(typeof(CompilationControl),
                new FrameworkPropertyMetadata(default(Compilation)));
        }
        public static readonly DependencyProperty CSharpCompilationOptionsProperty = DependencyProperty.Register(
            "CSharpCompilationOptions", typeof(CSharpCompilationOptions), typeof(CompilationControl), new PropertyMetadata(default(CSharpCompilationOptions)));
        public CSharpCompilation CSharpCompilation
            {
                get { return Compilation as CSharpCompilation; }
            }

            public CSharpCompilationOptions CSharpCompilationOptions
            {
                get { return (CSharpCompilationOptions) GetValue(CSharpCompilationOptionsProperty); }
                set { SetValue(CSharpCompilationOptionsProperty, value); }
            }


            /// <summary>
            /// 
            /// </summary>
            public static readonly DependencyProperty CompilationProperty = RoslynProperties.CompilationProperty;

            private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                var c = (CompilationControl) d;
                Compilation cc = (Compilation) e.NewValue;
                if (cc != null)
                {
                    c.DeclarationDiagnostics = cc.GetDeclarationDiagnostics().ToList();
                    c.Diagnostics = cc.GetDiagnostics().ToList();
                }
            }

            public IEnumerable<Diagnostic> DeclarationDiagnostics { get; set; }
            public IEnumerable<Diagnostic> Diagnostics { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Compilation Compilation
        {
            get { return (Compilation)GetValue(CompilationProperty); }
            set { SetValue(CompilationProperty, value); }
        }

    }
}