// <copyright file="DefaultAppDispatcher.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using Drastic.Services;

namespace MauiFeed.UI.Services;

public class DefaultAppDispatcher : Foundation.NSObject, IAppDispatcher
{
    public bool Dispatch(Action action)
    {
        this.InvokeOnMainThread(action);
        return true;
    }
}
