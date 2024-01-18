// <copyright file="TestDispatcherService.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

namespace MauiFeed.Core.Tests.Services;

public class TestDispatcherService : IAppDispatcher
{
    private SynchronizationContext context;

    public TestDispatcherService(SynchronizationContext context)
    {
        this.context = context;
    }

    public bool Dispatch(Action action)
    {
        this.context.Send(_ => action(), null);
        return true;
    }
}