using System.Collections.Generic;

namespace STUN.Message.Attributes
{
    public interface IAttribute
    {
        public IEnumerable<byte> Value { get; set; }
    }
}
