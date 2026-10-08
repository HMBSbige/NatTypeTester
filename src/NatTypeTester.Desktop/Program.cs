namespace NatTypeTester.Desktop;

internal static class Program
{
	/// <summary>
	/// Initialization code. Don't use any Avalonia, third-party APIs or any
	/// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	/// yet and stuff might break.
	/// </summary>
	[STAThread]
	public static int Main(string[] args)
	{
		try
		{
			return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}
		finally
		{
			AppLocator.GetLocator().Dispose();
		}
	}

	/// <summary>
	/// Avalonia configuration, don't remove; also used by visual designer.
	/// </summary>
	private static AppBuilder BuildAvaloniaApp()
	{
		return AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.UseNatTypeTesterApp()
			.LogToTrace()
			.With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.AngleEgl, Win32RenderingMode.Vulkan, Win32RenderingMode.Wgl, Win32RenderingMode.Software] })
			.With(new X11PlatformOptions { RenderingMode = [X11RenderingMode.Vulkan, X11RenderingMode.Egl, X11RenderingMode.Glx, X11RenderingMode.Software] })
			.With(static () => new FontManagerOptions { DefaultFamilyName = GetFallbackDefaultFontFamilyName() });
	}

	/// <summary>
	/// Workaround for the Skia (m119) bundled with SkiaSharp 3: its fontconfig backend only accepts a
	/// match among the first 16 family aliases, so the locale-preferred font (e.g. Noto Sans CJK SC
	/// under zh_CN) is rejected and SKTypeface.Default comes back empty. Avalonia then falls back to
	/// the alphabetically first installed family. Ask fontconfig for a font covering Latin text
	/// instead, which honors the locale.
	/// TODO: Remove after upgrading to SkiaSharp 4, whose Skia raises the limit to 65536.
	/// </summary>
	private static string? GetFallbackDefaultFontFamilyName()
	{
		if (!OperatingSystem.IsLinux() || !string.IsNullOrEmpty(SKTypeface.Default.FamilyName))
		{
			return null;
		}

		using SKTypeface? typeface = SKFontManager.Default.MatchCharacter('A');
		return typeface?.FamilyName;
	}
}
