using System.Diagnostics;

namespace MauiFeed.Core.Tests.Services;

public class TestErrorHandlerService : IErrorHandlerService
{
    private bool failOnError;

    public TestErrorHandlerService(bool failOnError = false)
    {
        this.failOnError = failOnError;
    }

    public void HandleError(Exception ex)
    {
        if (this.failOnError)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            Assert.Fail(ex.Message);
        }
    }
}