using System.Net;
using System.Net.Sockets;

namespace PanelExtractor
{
    internal static class HostValidator
    {
        public static bool IsInvalidIpv4Literal(string value)
        {
            bool looksLikeIpv4 = value.Length > 0 &&
                value.All(character => character is >= '0' and <= '9' || character == '.');

            if (!looksLikeIpv4)
            {
                return false;
            }

            return !IPAddress.TryParse(value, out IPAddress? address) ||
                address.AddressFamily != AddressFamily.InterNetwork ||
                value.Split('.').Length != 4;
        }
    }
}
