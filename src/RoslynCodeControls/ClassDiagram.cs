using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Threading;
#pragma warning disable 8618
#pragma warning disable 8625

namespace RoslynCodeControls
{
    public sealed class ClassDiagram : Control
    {
        /// <inheritdoc />
        public ClassDiagram()
        {
            AddHandler(EntityFigure.DragStartEvent, new RoutedEventHandler(DragStartEvent));
        }

        private void DragStartEvent(object sender, RoutedEventArgs e)
        {
            EntityFigure ef = (EntityFigure)e.OriginalSource;
            DraggingFigure = ef;
            Panel.SetZIndex(ef, DiagramPanel.Children.Cast<UIElement>().Max(z => Panel.GetZIndex(z)) + 1);
            DragStartPosition = Mouse.GetPosition(ef);
            CaptureMouse();
        }

        private Point DragStartPosition { get; set; }

        /// <inheritdoc />
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if(DraggingFigure != null)
            {
                DraggingFigure.Dragging = false;
                DraggingFigure = null;
                ReleaseMouseCapture();

            }
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (DraggingFigure != null)
            {
                var pos = e.GetPosition(this);
                pos.Offset(DragStartPosition.X * -1, DragStartPosition.Y * -1);
                Canvas.SetLeft(DraggingFigure, pos.X);
                Canvas.SetTop(DraggingFigure, pos.Y);

            }
        }

        private FigureControl DraggingFigure { get; set; }

        public static readonly DependencyProperty DocumentProperty = RoslynProperties.DocumentProperty;
        static ClassDiagram()
        {
            RoslynProperties.DocumentProperty.AddOwner(typeof(ClassDiagram), new PropertyMetadata(new PropertyChangedCallback(DocumentChanged)));
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ClassDiagram),
                new FrameworkPropertyMetadata(typeof(ClassDiagram)));

        }

        private static void DocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (ClassDiagram)d;
            c.OnDocumentChanged((Document)e.OldValue, (Document)e.NewValue);

        }

        private void OnDocumentChanged(Document oldValue, Document newValue)
        {
            if (newValue == null)
                return;
            if (DiagramPanel == null)
                return;
            JTF.RunAsync(() => SetupDocumentAsync(newValue));
            
            
        }

        private async Task SetupDocumentAsync(Document document)
        {

            var root = await document.GetSyntaxRootAsync();
            var walker = new Walker();
            walker.Visit(root);
            var nodes = walker.CompilationUnitNode.Children.OfType<NamespaceNode>().SelectMany(c => c.Children)
                .Concat(walker.CompilationUnitNode.Children.Where(c => !typeof(NamespaceNode).IsAssignableFrom(c.GetType())));
            foreach (var structureNode in nodes)
            {
                switch (structureNode)
                {
                    case ClassNode classNode:
                        if (!string.IsNullOrWhiteSpace(classNode.ClassIdentifier))
                        {
                            var entityFigure = FindFigure(classNode);
                            if (entityFigure == null)
                            {
                                entityFigure = new EntityFigure();
                                DiagramPanel.Children.Add(entityFigure);
                            }
                            entityFigure.ClassNode = classNode.Node;


                        }

                        break;
                    default:
                        break;
                }
		                
            }

            ArrangeFigures();

        }

        private EntityFigure FindFigure(ClassNode classNode)
        {
            foreach (UIElement diagramPanelChild in DiagramPanel.Children)
            {
                if (diagramPanelChild is EntityFigure ef)
                {
                    if (ef.EntityName == classNode.ClassIdentifier)
                    {
                        return ef;
                    }
                }
            }
            return null;
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            var arrangeOverride = base.ArrangeOverride(arrangeBounds);
            ArrangeFigures();
            
            return arrangeOverride;
        }

        private void ArrangeFigures()
        {
            double x = 0;
            double y = 0;
            double nextY = 0;
            foreach (UIElement child in DiagramPanel.Children)

            {
                if (child is Control c)
             
                {
                    if (x + c.ActualWidth > ActualWidth)
                    {

                        y = nextY;
                        x = 0;
                    }
                    Canvas.SetLeft(c,x);
                    Canvas.SetTop(c,y);
                    x += c.Width;
                    nextY = Math.Max(nextY, y + c.ActualHeight);
                }
            }
        }

        private JoinableTaskFactory JTF { get; set; } = new JoinableTaskFactory(new JoinableTaskContext());

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            DiagramPanel = (Panel) GetTemplateChild("DiagramPanel");
            
        }

        private Panel DiagramPanel { get; set; } = null!;

        public Document Document
        {
            get { return (Document)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }

    }
}