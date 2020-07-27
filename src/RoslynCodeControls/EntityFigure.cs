using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// ReSharper disable UnreachableCode

namespace RoslynCodeControls
{
    public sealed class EntityFigure : FigureControl
    {
        public static readonly RoutedEvent DragStartEvent = EventManager.RegisterRoutedEvent("DragStart",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(EntityFigure));

        public static readonly DependencyProperty EntityNameProperty = DependencyProperty.Register(
            "EntityName", typeof(string), typeof(EntityFigure), new PropertyMetadata(default(string)));


        public string EntityName
        {
            get { return (string) GetValue(EntityNameProperty); }
            set { SetValue(EntityNameProperty, value); }
        }

        public static readonly DependencyProperty EntityMembersProperty = DependencyProperty.Register(
            "EntityMembers", typeof(IEnumerable), typeof(EntityFigure), new PropertyMetadata(default(IEnumerable)));

        public IEnumerable EntityMembers
        {
            get { return (IEnumerable) GetValue(EntityMembersProperty); }
            set { SetValue(EntityMembersProperty, value); }
        }

        public static readonly DependencyProperty MembersCollectionViewProperty = DependencyProperty.Register(
            "MembersCollectionView", typeof(ICollectionView), typeof(EntityFigure), new PropertyMetadata(default(CollectionView)));

        public ICollectionView MembersCollectionView
        {
            get { return (ICollectionView) GetValue(MembersCollectionViewProperty); }
            set { SetValue(MembersCollectionViewProperty, value); }
        }
        static EntityFigure()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(EntityFigure),
                new FrameworkPropertyMetadata(typeof(EntityFigure)));
        }

        public static readonly DependencyProperty ClassNodeProperty = DependencyProperty.Register(
            "ClassNode", typeof(SyntaxNode), typeof(EntityFigure),
            new PropertyMetadata(default(SyntaxNode), OnClassNodeChanged));

        private UIElement _titleTextBlock;

        public SyntaxNode ClassNode
        {
            get { return (SyntaxNode) GetValue(ClassNodeProperty); }
            set { SetValue(ClassNodeProperty, value); }
        }

        private static void OnClassNodeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((EntityFigure) d).OnClassNodeChanged((SyntaxNode) e.OldValue, (SyntaxNode) e.NewValue);
        }


        private void OnClassNodeChanged(SyntaxNode oldValue, SyntaxNode newValue)
        {
            if (newValue == null) return;
            if (!(newValue is ClassDeclarationSyntax cds)) return;
            EntityName = cds.Identifier.Text;
            var methods = new List<MethodEntityMember>();
            var constructors = new List<ConstructorEntityMember>();
            var properties = new List<PropertyEntityMember>();
            foreach (var memberDeclarationSyntax in cds.Members)
            {
                EntityMember entityMember = null!;
                switch (memberDeclarationSyntax)
                {
                    case EventFieldDeclarationSyntax eventFieldDeclarationSyntax:
                        break;
                    case FieldDeclarationSyntax fieldDeclarationSyntax:
                        break;
                    case ConstructorDeclarationSyntax constructorDeclarationSyntax:
                    {
                        const bool includeType = false;
                        var p = constructorDeclarationSyntax.ParameterList.Parameters.Select(p1 =>
                            (includeType ? p1.Type?.ToString() + " " ?? "" : "") + p1.Identifier.Text);
                        var ps = string.Join(", ", p);

                        var stereotypes = new List<string>();
                        var staticFlag = constructorDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword);
                        if (staticFlag) stereotypes.Add("static");

                        var codePreview = GetCodePreview(constructorDeclarationSyntax);

                        var accessibilitySymbol = GetAccessibilitySymbol(constructorDeclarationSyntax);
                        constructors.Add(new ConstructorEntityMember()
                        {
                            Name = constructorDeclarationSyntax.Identifier.Text, Parameters = ps,
                            AccessibilitySymbol = accessibilitySymbol,
                            Suffix =
                                stereotypes.Any() ? " \x0ab" + string.Join(",", stereotypes) + "\x0bb" : "",
                            Location = constructorDeclarationSyntax.GetLocation(),
                            CodePreview = codePreview
                        });
                    }
                        break;
                    case ConversionOperatorDeclarationSyntax conversionOperatorDeclarationSyntax:
                        break;
                    case DestructorDeclarationSyntax destructorDeclarationSyntax:
                        break;
                    case MethodDeclarationSyntax methodDeclarationSyntax:
                    {
                        const bool includeType = false;
                        var p = methodDeclarationSyntax.ParameterList.Parameters.Select(p =>
                            (includeType ? p.Type?.ToString() + " " ?? "" : "") + p.Identifier.Text);
                        var ps = string.Join(", ", p);
                        var accessibilitySymbol = GetAccessibilitySymbol(memberDeclarationSyntax);
                        var staticFlag = memberDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword);
                        var asyncFlag = memberDeclarationSyntax.Modifiers.Any(SyntaxKind.AsyncKeyword);
                        var stereotypes = new List<string>();
                        if (staticFlag) stereotypes.Add("static");

                        if (asyncFlag)
                            stereotypes.Add("async");

                        var codePreview = GetCodePreview(methodDeclarationSyntax);

                        var method = new MethodEntityMember()
                        {
                            Name = methodDeclarationSyntax.Identifier.Text,
                            Node = methodDeclarationSyntax,
                            Parameters = ps,
                            AccessibilitySymbol = accessibilitySymbol,
                            Suffix =
                                stereotypes.Any() ? " \x0ab" + string.Join(",", stereotypes) + "\x0bb" : "",
                            Location = methodDeclarationSyntax.GetLocation(),
                            CodePreview = codePreview
                        };
                        methods.Add(method);
                    }
                        break;

                    case OperatorDeclarationSyntax operatorDeclarationSyntax:
                        break;
                    // case BaseMethodDeclarationSyntax baseMethodDeclarationSyntax:
                    // break;
                    case EventDeclarationSyntax eventDeclarationSyntax:
                        break;
                    case IndexerDeclarationSyntax indexerDeclarationSyntax:
                        break;
                    case PropertyDeclarationSyntax propertyDeclarationSyntax:
                    {
                        var accessibilitySymbol = GetAccessibilitySymbol(propertyDeclarationSyntax);

                        var propMember = new PropertyEntityMember
                        {
                            Name = propertyDeclarationSyntax.Identifier.Text,
                            Node = propertyDeclarationSyntax,
                            AccessibilitySymbol = accessibilitySymbol
                        };
                        properties.Add(propMember);
                    }
                        break;
                    case BasePropertyDeclarationSyntax basePropertyDeclarationSyntax:
                        break;
                    case ClassDeclarationSyntax classDeclarationSyntax:
                        break;
                    case EnumDeclarationSyntax enumDeclarationSyntax:
                        break;
                    case InterfaceDeclarationSyntax interfaceDeclarationSyntax:
                        break;
                    case StructDeclarationSyntax structDeclarationSyntax:
                        break;
                    case TypeDeclarationSyntax typeDeclarationSyntax:
                        break;
                    case BaseTypeDeclarationSyntax baseTypeDeclarationSyntax:
                        break;
                    case DelegateDeclarationSyntax delegateDeclarationSyntax:
                        break;
                    case EnumMemberDeclarationSyntax enumMemberDeclarationSyntax:
                        break;
                    case GlobalStatementSyntax globalStatementSyntax:
                        break;
                    case IncompleteMemberSyntax incompleteMemberSyntax:
                        break;
                    case NamespaceDeclarationSyntax namespaceDeclarationSyntax:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(memberDeclarationSyntax));
                }
            }

            foreach (var constructorEntityMember in constructors)
                InternalEntityMembers.Add(constructorEntityMember);
            foreach (var member in properties)
                InternalEntityMembers.Add(member);

            foreach (var methodEntityMember in methods) InternalEntityMembers.Add(methodEntityMember);
        }

        private static string GetCodePreview(BaseMethodDeclarationSyntax baseMethod)
        {
            int l;
            if (baseMethod.Body != null)
                l = baseMethod.Body.OpenBraceToken.Span.End;
            else
                l = baseMethod.ExpressionBody.ArrowToken.GetPreviousToken()
                    .Span.End;

            string codePreview = baseMethod.SyntaxTree.ToString()
                .Substring(baseMethod.SpanStart,
                    l - baseMethod.SpanStart);
            return codePreview;
        }

        private static string GetAccessibilitySymbol(MemberDeclarationSyntax memberDeclarationSyntax)
        {
            var accessibilitySymbol =
                memberDeclarationSyntax.Modifiers.Any(z =>
                    z.RawKind == (int) SyntaxKind.PublicKeyword)
                    ? "+"
                    : memberDeclarationSyntax.Modifiers.Any(z =>
                        z.RawKind == (int) SyntaxKind.PrivateKeyword)
                        ? "-"
                        : "";
            return accessibilitySymbol;
        }

        /// <inheritdoc />
#pragma warning disable 8618
        public EntityFigure()
#pragma warning restore 8618
        {
            InternalEntityMembers = new ObservableCollection<EntityMember>();
            EntityMembers = InternalEntityMembers;
            MembersCollectionView = CollectionViewSource.GetDefaultView(
                InternalEntityMembers);
            MembersCollectionView.GroupDescriptions.Add(new PropertyGroupDescription("MemberType"));
            // CommandBindings.Add(new CommandBinding(ApplicationCommands.Find, Find))
        }


        private ObservableCollection<EntityMember> InternalEntityMembers { get; set; }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            TitleTextBlock = (UIElement) GetTemplateChild("TitleTextBlock");
        }

        public UIElement TitleTextBlock
        {
            get { return _titleTextBlock; }
            set
            {
                _titleTextBlock = value;
                _titleTextBlock.MouseMove += TitleTextBlockOnMouseMove;
                _titleTextBlock.MouseUp += TitleTextBlockOnMouseUp;
            }
        }

        private void TitleTextBlockOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            // if (Dragging)
            // {
            // Dragging = false;
            // ReleaseMouseCapture();
            // }
        }

        private void TitleTextBlockOnMouseMove(object sender, MouseEventArgs e)
        {
            if (!Dragging && e.LeftButton == MouseButtonState.Pressed)
            {
                RaiseEvent(new RoutedEventArgs(DragStartEvent, this));
                Dragging = true;
                DraggingOrigin = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
            }

            // else if (Dragging)
            // {
            // var dependencyObject = this.VisualParent;
            // if (dependencyObject != null)
            // {
            // var pos = e.GetPosition((IInputElement) dependencyObject);
            // Canvas.SetLeft(this, pos.X);
            // Canvas.SetTop(this, pos.Y);
            // }
            // }
        }

        public Point DraggingOrigin { get; set; }
    }

    public class PropertyEntityMember : EntityMember
    {
    }
}