namespace TrojanPlusApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp(Action<Microsoft.Maui.Hosting.IMauiHandlersCollection>? configureHandlers = null)
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		if (configureHandlers != null)
		{
			builder.ConfigureMauiHandlers(configureHandlers);
		}

		return builder.Build();
	}
}