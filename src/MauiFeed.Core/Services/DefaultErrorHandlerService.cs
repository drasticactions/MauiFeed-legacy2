// <copyright file="DefaultErrorHandlerService.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using Drastic.Services;

namespace MauiFeed.Services;

public class DefaultErrorHandlerService : IErrorHandlerService
{
    public void HandleError(Exception ex)
    {
        System.Diagnostics.Debug.WriteLine(ex);
    }
}
