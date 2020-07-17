using System.Windows;
using System.Windows.Controls;

namespace RoslynCodeControls
{
    public static class DocumentProperties
    {
        public static readonly DependencyProperty DocumentTitleProperty = DependencyProperty.RegisterAttached(
            "DocumentTitle", typeof(string), typeof(DocumentProperties), new PropertyMetadata(default(string)));

        public static string GetDocumentTitle(DependencyObject d)
        {
            return (string) d.GetValue(DocumentTitleProperty);
        }

        public static void SetDocumentTitle(DependencyObject d, string title)
        {
            d.SetValue(DocumentTitleProperty, title);
        }
        // public string DocumentTitle
        // {
        //     get { return (string) GetValue(DocumentTitleProperty); }
        //     set { SetValue(DocumentTitleProperty, value); }
        // }
    }

    /// <inheritdoc />
    public class CoverPageDocument : Control
    {

        public static readonly DependencyProperty DocumentTitleProperty = DocumentProperties.DocumentTitleProperty;
        static CoverPageDocument()
        {
            DocumentProperties.DocumentTitleProperty.AddOwner(typeof(CoverPageDocument));
        }
        public string DocumentTitle
        {
        get { return (string) GetValue(DocumentTitleProperty); }
        set { SetValue(DocumentTitleProperty, value); }
        }

    }

    }