using Avalonia;
using Avalonia.Markup.Xaml;

namespace Apachi.AvaloniaApp
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
