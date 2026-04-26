using System.Security.Cryptography.X509Certificates;

namespace STUN.Proxy;

internal static class CertificateChainDisposer
{
	public static void DisposeContents(X509Chain? chain)
	{
		if (chain is null)
		{
			return;
		}

		foreach (X509Certificate2 extraCert in chain.ChainPolicy.ExtraStore)
		{
			extraCert.Dispose();
		}

		foreach (X509ChainElement element in chain.ChainElements)
		{
			element.Certificate.Dispose();
		}
	}
}
