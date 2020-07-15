using System.Windows.Documents;

namespace RoslynCodeControls
{
    public class PaginatingRoslynCodeControl : RoslynCodeControl, IDocumentPaginatorSource
    {
        private readonly RoslynPaginator _documentPaginator;
        
        public PaginatingRoslynCodeControl()
        {
            _documentPaginator = new RoslynPaginator(this);
            PixelsPerDip = 1.0;
            UpdateTextSource();
        }

        public DocumentPaginator DocumentPaginator
        {
            get
            {

                return _documentPaginator;
            }
        }
    }
}