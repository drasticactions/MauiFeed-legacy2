using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MauiFeed.MauiUI.ViewModels;

namespace MauiFeedMobile;

public partial class DebugPage : ContentPage
{
    private DebugViewModel vm;

    public DebugPage(IServiceProvider provider)
    {
        InitializeComponent();
        this.BindingContext = this.vm = new DebugViewModel(provider);
    }
}