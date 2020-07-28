using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using AvalonDock.Layout;
using Microsoft.CodeAnalysis;
using RoslynCodeControls;

namespace MultiDoc
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty DefaultWorkspaceProperty = DependencyProperty.Register("DefaultWorkspace", typeof(Workspace), typeof(MainWindow), new PropertyMetadata(default(Workspace)));
        
        public MainWindow()
        {
            InitializeComponent();
            Workspaces = _internalWorkspacesCollection;
            var defaultWorkspace = new AdhocWorkspace();
            _internalWorkspacesCollection.Add(defaultWorkspace);
            DefaultWorkspace = defaultWorkspace;
            AddHandler(RoslynProperties.WorkspaceUpdatedEvent, new WorkspaceUpdatedEventHandler(WorkspaceUpdatedEventHandler));
        }

        private void WorkspaceUpdatedEventHandler(object sender, WorkspaceUpdatedEventArgs e)

        {
            CollectionViewSource.GetDefaultView(Workspaces).Refresh();
            CollectionViewSource.GetDefaultView(e.Workspace.CurrentSolution.Projects).Refresh();
            WorkspacesMenu.ItemsSource = null;
            WorkspacesMenu.ItemsSource = Workspaces;
            return;
            foreach (var workspacesMenuItem in WorkspacesMenu.Items)
            {
                var container0 = WorkspacesMenu.ItemContainerGenerator.ContainerFromItem(workspacesMenuItem);

            }
            int i = 0;
            foreach (object item in WorkspacesMenu.ItemContainerGenerator.Items)
            {
                if (Equals(item, e.Workspace))
                {
                    var container0 = WorkspacesMenu.ItemContainerGenerator.ContainerFromIndex(i);
                }

                i++;
            }
            var container = WorkspacesMenu.ItemContainerGenerator.ContainerFromItem(e.Workspace);
            if (container is MenuItem menuItem)
            {
                menuItem.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateTarget();
            }
            // WorkspacesMenu.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateTarget();
        }


        public static readonly DependencyProperty WorkspacesProperty = DependencyProperty.Register(
            "Workspaces", typeof(IEnumerable), typeof(MainWindow), new PropertyMetadata(default(IEnumerable)));

        public IEnumerable Workspaces
        {
            get { return (IEnumerable) GetValue(WorkspacesProperty); }
            set { SetValue(WorkspacesProperty, value); }
        }

        private ObservableCollection<Workspace> _internalWorkspacesCollection = new ObservableCollection<Workspace>();

        public Workspace DefaultWorkspace
        {
            get { return (Workspace) GetValue(DefaultWorkspaceProperty); }
            set { SetValue(DefaultWorkspaceProperty, value); }
        }

        private void OnClassDiagramCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (!(e.Parameter is RoslynCodeBase rcb)) return;
            var classDiagram = new ClassDiagram();
            classDiagram.SetBinding(ClassDiagram.DocumentProperty, new Binding("Document") {Source = rcb});
            var layoutDocument = new LayoutDocument(){Title = "Class Diagram",Content = classDiagram};
            LayoutDocumentPane.Children.Add(layoutDocument);
            DockingManager.ActiveContent = layoutDocument;
        }
    }
}
