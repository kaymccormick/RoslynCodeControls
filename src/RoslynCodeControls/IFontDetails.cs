using System.Windows;

namespace RoslynCodeControls
{
    public interface IFontDetails
    {
        double PixelsPerDip { get; }
        double FontSize { get; }
        string FaceName { get; }
        FontWeight FontWeight { get; }
    }
}