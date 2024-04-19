namespace Apachi.ViewModels;

public class PageViewModel : Screen
{
    public new MainViewModel? Parent => (MainViewModel?)base.Parent;
}
