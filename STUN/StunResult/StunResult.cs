using System.Net;

namespace STUN.StunResult;

public abstract record StunResult
{
	public IPEndPoint? PublicEndPoint { get; set; }
	public IPEndPoint? LocalEndPoint { get; set; }
}
