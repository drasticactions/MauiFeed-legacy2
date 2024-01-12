using Drastic.Services;

namespace MauiFeed.UI.Services;

public class DefaultErrorHandlerService : IErrorHandlerService
{
    public void HandleError(Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(ex);
    }
}