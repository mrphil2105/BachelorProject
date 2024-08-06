namespace Apachi.ViewModels;

public interface IMenuPageViewModel
{
    string PageName { get; }

    int PageNumber { get; }

    bool IsReviewer { get; }
}
