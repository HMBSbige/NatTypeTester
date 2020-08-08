using STUN.Enums;
using STUN.Message;
using STUN.Message.Attributes;
using System.Linq;
using System.Net;

namespace STUN.Utils
{
    public static class AttributeExtensions
    {
        public static Attribute BuildChangeRequest(bool changeIp, bool changePort)
        {
            return new Attribute
            {
                Type = AttributeType.ChangeRequest,
                Length = 4,
                Value = new ChangeRequestAttribute { ChangeIp = changeIp, ChangePort = changePort }
            };
        }

        public static IPEndPoint GetMappedAddressAttribute(StunMessage5389 response)
        {
            var mappedAddressAttribute = response?.Attributes.FirstOrDefault(t => t.Type == AttributeType.MappedAddress);

            if (mappedAddressAttribute == null) return null;

            var mapped = (MappedAddressAttribute)mappedAddressAttribute.Value;
            return new IPEndPoint(mapped.Address, mapped.Port);
        }

        public static IPEndPoint GetChangedAddressAttribute(StunMessage5389 response)
        {
            var changedAddressAttribute = response?.Attributes.FirstOrDefault(t => t.Type == AttributeType.ChangedAddress);

            if (changedAddressAttribute == null) return null;

            var address = (ChangedAddressAttribute)changedAddressAttribute.Value;
            return new IPEndPoint(address.Address, address.Port);
        }

        public static IPEndPoint GetXorMappedAddressAttribute(StunMessage5389 response)
        {
            var mappedAddressAttribute = response?.Attributes.FirstOrDefault(t => t.Type == AttributeType.XorMappedAddress) ??
                                         response?.Attributes.FirstOrDefault(t => t.Type == AttributeType.MappedAddress);

            if (mappedAddressAttribute == null) return null;

            var mapped = (AddressAttribute)mappedAddressAttribute.Value;
            return new IPEndPoint(mapped.Address, mapped.Port);
        }

        public static IPEndPoint GetOtherAddressAttribute(StunMessage5389 response)
        {
            var addressAttribute = response?.Attributes.FirstOrDefault(t => t.Type == AttributeType.OtherAddress)
                                   ?? response?.Attributes.FirstOrDefault(t => t.Type == AttributeType.ChangedAddress);

            if (addressAttribute == null) return null;

            var address = (AddressAttribute)addressAttribute.Value;
            return new IPEndPoint(address.Address, address.Port);
        }
    }
}
