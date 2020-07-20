﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
using Microsoft.VisualStudio.Threading;
using RoslynCodeControls;

namespace WpfTestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedUICommand HideToolBar = new RoutedUICommand("Hide toolbar", nameof(HideToolBar), typeof(MainWindow));
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


        public static readonly DependencyProperty FontsProperty = DependencyProperty.Register(
            "Fonts", typeof(IEnumerable), typeof(MainWindow),
            new PropertyMetadata(default(IEnumerable), null, CoerceFontsValue));

        private static object CoerceFontsValue(DependencyObject d, object basevalue)
        {
            return System.Windows.Media.Fonts.SystemFontFamilies;
        }

        public IEnumerable Fonts
        {
            get { return (IEnumerable) GetValue(FontsProperty); }
            set { SetValue(FontsProperty, value); }
        }

        public static readonly DependencyProperty DefaultHideToolBarCommandProperty = DependencyProperty.Register(
            "DefaultHideToolBarCommand", typeof(ICommand), typeof(MainWindow), new PropertyMetadata(HideToolBar));

        public ICommand DefaultHideToolBarCommand
        {
            get { return (ICommand) GetValue(DefaultHideToolBarCommandProperty); }
            set { SetValue(DefaultHideToolBarCommandProperty, value); }
        }

        public JoinableTaskFactory JTF2 { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            CoerceValue(FontsProperty);
            Loaded += OnLoaded;
            //CodeControl.Filename = @"c:\temp\dockingmanager.cs";
           
            CommandBindings.Add(new CommandBinding(HideToolBar, OnExecutedHideToolBar));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            JTF.RunAsync(M1);
        }

        private async Task M1()
        {
            CodeControl.JTF = JTF;
            CodeControl.JTF2 = JTF2;
            CodeControl.SourceText = File.ReadAllText(@"C:\temp\dockingmanager.cs");
            CodeControl.AddHandler(RoslynCodeControl.RenderStartEvent, new RoutedEventHandler((sender, args) =>
            {
                StartTime = DateTime.Now;
                Debug.WriteLine("render start");
            }));
            CodeControl.AddHandler(RoslynCodeControl.RenderCompleteEvent, new RoutedEventHandler((sender, args) =>
            {
                var span = DateTime.Now - StartTime;
                Debug.WriteLine("render complete " + span);
            }));
            await CodeControl.UpdateFormattedTextAsync();
        }

        public DateTime StartTime { get; set; }

        public JoinableTaskFactory JTF { get; set; } = new JoinableTaskFactory(new JoinableTaskContext());

        private async void FontComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CodeControl.FontFamily = (FontFamily) FontComboBox.SelectedItem;
            await CodeControl.UpdateTextSourceAsync();
        }

        private async void FontSizeCombo_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CodeControl.FontSize = (double) FontSizeCombo.SelectedItem;
            await CodeControl.UpdateTextSourceAsync();
        }

        private void OnExecutedHideToolBar(object sender, ExecutedRoutedEventArgs e)
        {
            
        }

        private void CommandBinding_OnCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            
        }
    }
}