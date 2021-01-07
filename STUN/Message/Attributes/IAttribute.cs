using System.Collections.Generic;

namespace STUN.Message.Attributes
{
	public interface IAttribute
	{
		public IEnumerable<byte> Bytes { get; }

		public bool TryParse(byte[] bytes);
	}
}
