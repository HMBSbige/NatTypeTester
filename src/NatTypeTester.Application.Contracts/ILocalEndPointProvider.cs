namespace NatTypeTester.Application.Contracts;

public interface ILocalEndPointProvider
{
	IReadOnlyList<string> GetLocalEndPoints();
}
