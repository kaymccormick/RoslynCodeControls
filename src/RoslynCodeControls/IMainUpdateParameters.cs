using System.Windows;

namespace RoslynCodeControls
{
    public interface IMainUpdateParameters
    {
        double PixelsPerDip { get; }
        FontWeight FontWeight { get; }
    }
}