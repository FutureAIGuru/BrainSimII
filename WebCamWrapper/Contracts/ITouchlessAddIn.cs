using System.Windows;

namespace Touchless.Vision.Contracts
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Touchless")]
    public interface ITouchlessAddIn
    {
        string Name { get; }
        string Description { get; }
        bool HasConfiguration { get; }
        UIElement ConfigurationElement { get; }
    }
}