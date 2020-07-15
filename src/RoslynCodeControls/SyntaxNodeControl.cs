using System.Linq;
using System.Windows;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace RoslynCodeControls
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class SyntaxNodeControl : CompilationControl
    {
        static SyntaxNodeControl()
        {
            RoslynProperties.SourceTextProperty.AddOwner(typeof(SyntaxNodeControl), new PropertyMetadata(default(string), OnSourceTextUpdated));
            RoslynProperties.SemanticModelProperty.AddOwner(typeof(SyntaxNodeControl));
            RoslynProperties.CompilationProperty.AddOwner(typeof(SyntaxNodeControl));
            RoslynProperties.DocumentProperty.AddOwner(typeof(SyntaxNodeControl));
            RoslynProperties.SyntaxNodeProperty.AddOwner(typeof(SyntaxNodeControl));
            RoslynProperties.SyntaxTreeProperty.AddOwner(typeof(SyntaxNodeControl));

        }
        /// <summary>
        /// 
        /// </summary>
        ///
        public static readonly DependencyProperty DocumentProperty = RoslynProperties.DocumentProperty;
            
        public Document Document
        {
            get { return (Document) GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }

        public static readonly DependencyProperty IsSelectingProperty = DependencyProperty.Register(
            "IsSelecting", typeof(bool), typeof(SyntaxNodeControl), new PropertyMetadata(default(bool)));

        /// <summary>
        /// 
        /// </summary>
        public bool IsSelecting
        {
            get { return (bool) GetValue(IsSelectingProperty); }
            set { SetValue(IsSelectingProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty SelectionEnabledProperty = DependencyProperty.Register(
            "SelectionEnabled", typeof(bool), typeof(SyntaxNodeControl), new PropertyMetadata(true));

        /// <summary>
        /// 
        /// </summary>
        public bool SelectionEnabled
        {
            get { return (bool) GetValue(SelectionEnabledProperty); }
            set { SetValue(SelectionEnabledProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty SyntaxNodeProperty = RoslynProperties.SyntaxNodeProperty;

        /// <summary>
        /// 
        /// </summary>
        public SyntaxNode SyntaxNode
        {
            get { return (SyntaxNode) GetValue(SyntaxNodeProperty); }
            set { SetValue(SyntaxNodeProperty, value); }
        }


        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty SyntaxTreeProperty = RoslynProperties.SyntaxTreeProperty;

        /// <summary>
        /// 
        /// </summary>
        public SyntaxTree SyntaxTree
        {
            get { return (SyntaxTree) GetValue(SyntaxTreeProperty); }
            set { SetValue(SyntaxTreeProperty, value); }
        }


        /// <summary>
        /// 
        /// </summary>
        public static readonly DependencyProperty SourceTextProperty = RoslynProperties.SourceTextProperty;

        public static readonly DependencyProperty SemanticModelProperty = RoslynProperties.SemanticModelProperty;

        public SemanticModel SemanticModel
        {
            get { return (SemanticModel) GetValue(SemanticModelProperty); }
            set { SetValue(SemanticModelProperty, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public string SourceText
        {
            get { return (string) GetValue(SourceTextProperty); }
            set { SetValue(SourceTextProperty, value); }
        }

        protected bool UpdatingSourceText { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public bool ChangingText { get; set; }

        public static readonly DependencyProperty FilenameProperty = DependencyProperty.Register(
            "Filename", typeof(string), typeof(SyntaxNodeControl), new PropertyMetadata(default(string), OnFilenameChanged));

        public string Filename
        {
            get { return (string) GetValue(FilenameProperty); }
            set { SetValue(FilenameProperty, value); }
        }

        private static void OnFilenameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SyntaxNodeControl) d).OnFilenameChanged((string) e.OldValue, (string) e.NewValue);
        }

        protected virtual void OnFilenameChanged(string oldValue, string newValue)
        {
        }


        private static void OnSourceTextUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var f = (SyntaxNodeControl) d;

            f.OnSourceTextChanged1((string) e.NewValue, (string) e.OldValue);
        }

        protected virtual async void OnSourceTextChanged1(string newValue, string eOldValue)
        {
            if (ChangingText || UpdatingSourceText)
                return;
            if (newValue != null)
            {
                UpdatingSourceText = true;
                var compilation = CSharpCompilation.Create(
                    "test",
                    new[]
                    {
                        SyntaxFactory.ParseSyntaxTree(
                            newValue)
                    }, new[] {MetadataReference.CreateFromFile(typeof(object).Assembly.Location)},
                    CSharpCompilationOptions);
                Compilation = compilation;
                SyntaxTree = compilation.SyntaxTrees.First();
                
                SyntaxNode = await SyntaxTree.GetRootAsync().ConfigureAwait(true);
                SemanticModel = compilation.GetSemanticModel(SyntaxTree);
                UpdatingSourceText = false;
            }
        }
    }
}