using Drastic.Services;

namespace MauiFeed.MauiUI.Services;

public class MauiErrorHandler : IErrorHandlerService
{
    public void HandleError(Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(ex.Message);
    }
}