﻿using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Threading;

namespace RoslynCodeControls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:AnalysisControls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:AnalysisControls;assembly=AnalysisControls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:EnhancedCodeControl/>
    ///
    /// </summary>
    public class EnhancedCodeControl : SyntaxNodeControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty FontsProperty = DependencyProperty.Register(
            "Fonts", typeof(IEnumerable), typeof(EnhancedCodeControl),
            new PropertyMetadata(default(IEnumerable), OnFontsChanged));

        public IEnumerable Fonts
        {
            get { return (IEnumerable) GetValue(FontsProperty); }
            set { SetValue(FontsProperty, value); }
        }

        private static void OnFontsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EnhancedCodeControl) d).OnFontsChanged((IEnumerable) e.OldValue, (IEnumerable) e.NewValue);
        }


        protected virtual void OnFontsChanged(IEnumerable oldValue, IEnumerable newValue)
        {
        }


        /// <inheritdoc />
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            // if (CodeControl != null) Keyboard.Focus(CodeControl);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            return base.ArrangeOverride(arrangeBounds);
        }

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius) GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }


        public static readonly DependencyProperty CornerRadiusProperty = Border.CornerRadiusProperty;
        static EnhancedCodeControl()
        {
            Border.CornerRadiusProperty.AddOwner(typeof(EnhancedCodeControl));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EnhancedCodeControl),
                new FrameworkPropertyMetadata(typeof(EnhancedCodeControl)));
            // CompilationProperty.AddOwner(typeof(EnhancedCodeControl));
//            SyntaxNodeControl.CompilationProperty.OverrideMetadata(typeof(EnhancedCodeControl), new PropertyMetadata(null, PropertyChangedCallback));
// ModelProperty.OverrideMetadata(typeof(EnhancedCodeControl), new PropertyMetadata(SemanticModelChanged));
// SyntaxTreeProperty.OverrideMetadata(typeof(EnhancedCodeControl), new FrameworkPropertyMetadata(SyntaxTreeChanged));
// SyntaxNodeProperty.OverrideMetadata(typeof(EnhancedCodeControl), new PropertyMetadata(SyntaxNodeChanged));
        }

        private static void SyntaxNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var node = (SyntaxNode) e.NewValue;
            if (node is CSharpSyntaxNode csn)
            {
                var w = new Walker();
                w.Visit(node);
                var rootNode = w.CompilationUnitNode;
            }
        }

        private static void SyntaxTreeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tree = (SyntaxTree) e.NewValue;
        }

        private static void SemanticModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var model = (SemanticModel) e.NewValue;
        }

        /// <inheritdoc />
        public EnhancedCodeControl()
        {
            var observableCollection = new ObservableCollection<FontFamily>();
            foreach (var systemFontFamily in System.Windows.Media.Fonts.SystemFontFamilies)
                observableCollection.Add(systemFontFamily);
            Fonts = observableCollection;
        }

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            CodeControl = (RoslynCodeControl) GetTemplateChild("CodeControl");
            FontSizeCombo = (ComboBox) GetTemplateChild("FontSizeCombo");
            FontCombo = (ComboBox) GetTemplateChild("FontComboBox");
        }

        public ComboBox FontCombo
        {
            get { return _fontCombo; }
            set
            {
                _fontCombo = value;
                if (_fontCombo != null) _fontCombo.SelectionChanged += FontComboOnSelectionChanged;
            }
        }

        private async void FontComboOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CodeControl.FontFamily = (FontFamily) FontCombo.SelectedItem;
            await CodeControl.UpdateTextSourceAsync();
        }


        public ComboBox FontSizeCombo
        {
            get { return _fontSizeCombo; }
            set
            {
                _fontSizeCombo = value;
                if (_fontSizeCombo != null) _fontSizeCombo.SelectionChanged += FontSizeComboOnSelectionChanged;
            }
        }

        private async void FontSizeComboOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CodeControl.FontSize = (double) FontSizeCombo.SelectedItem;
            
           
            
        }

        public static readonly DependencyProperty CodeControlProperty = DependencyProperty.Register(
            "CodeControl", typeof(RoslynCodeControl), typeof(EnhancedCodeControl),
            new PropertyMetadata(default(RoslynCodeControl), OnCodeControlChanged));

        private ComboBox _fontCombo;
        private ComboBox _fontSizeCombo;
        private JoinableTaskFactory _jtf2;
        private AdhocWorkspace _workspace;
        private int _debugLevel;
        public static readonly DependencyProperty EnableToolBarsProperty = DependencyProperty.Register("EnableToolBars", typeof(bool), typeof(EnhancedCodeControl), new PropertyMetadata(default(bool)));

        public RoslynCodeControl CodeControl
        {
            get { return (RoslynCodeControl) GetValue(CodeControlProperty); }
            set { SetValue(CodeControlProperty, value); }
        }

        private static void OnCodeControlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EnhancedCodeControl) d).OnCodeControlChanged((RoslynCodeControl) e.OldValue,
                (RoslynCodeControl) e.NewValue);
        }


        protected virtual void OnCodeControlChanged(RoslynCodeControl oldValue, RoslynCodeControl newValue)
        {
        }


        public static double[] CommonFontSizes
        {
            get
            {
                return new double[]
                {
                    3.0d, 4.0d, 5.0d, 6.0d, 6.5d, 7.0d, 7.5d, 8.0d, 8.5d, 9.0d,
                    9.5d, 10.0d, 10.5d, 11.0d, 11.5d, 12.0d, 12.5d, 13.0d, 13.5d, 14.0d,
                    15.0d, 16.0d, 17.0d, 18.0d, 19.0d, 20.0d, 22.0d, 24.0d, 26.0d, 28.0d,
                    30.0d, 32.0d, 34.0d, 36.0d, 38.0d, 40.0d, 44.0d, 48.0d, 52.0d, 56.0d,
                    60.0d, 64.0d, 68.0d, 72.0d, 76.0d, 80.0d, 88.0d, 96.0d, 104.0d, 112.0d,
                    120.0d, 128.0d, 136.0d, 144.0d, 152.0d, 160.0d, 176.0d, 192.0d, 208.0d,
                    224.0d, 240.0d, 256.0d, 272.0d, 288.0d, 304.0d, 320.0d, 352.0d, 384.0d,
                    416.0d, 448.0d, 480.0d, 512.0d, 544.0d, 576.0d, 608.0d, 640.0d
                };
            }
        }

        public JoinableTaskFactory JTF2
        {
            get { return _jtf2; }
            set
            {
                _jtf2 = value;
                CodeControl.JTF2 = value;
            }
        }

        public bool EnableToolBars
        {
            get { return (bool) GetValue(EnableToolBarsProperty); }
            set { SetValue(EnableToolBarsProperty, value); }
        }

        public AdhocWorkspace Workspace
        {
            get { return _workspace; }
            set
            {
                if (Equals(value, _workspace)) return;
                _workspace = value;
                CodeControl.Workspace = value;
                OnPropertyChanged();
            }
        }

        public int DebugLevel
        {
            get { return _debugLevel; }
            set
            {
                _debugLevel = value;
                CodeControl.DebugLevel = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public Task UpdateFormattedTextAsync()
        {
            return CodeControl.UpdateFormattedTextAsync();
        }
    }
}