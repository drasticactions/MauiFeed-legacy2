namespace MauiFeedMobile;

public partial class App : Application
{
	public App(IServiceProvider provider)
	{
		InitializeComponent();

		MainPage = new DebugPage(provider);
	}
}
