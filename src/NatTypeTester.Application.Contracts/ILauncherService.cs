namespace NatTypeTester.Application.Contracts;

public interface ILauncherService
{
	ValueTask LaunchUriAsync(Uri uri);
}
