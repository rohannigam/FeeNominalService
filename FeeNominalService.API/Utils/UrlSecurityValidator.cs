using System;
using System.Net;
using System.Net.Sockets;

namespace FeeNominalService.Utils
{
    public static class UrlSecurityValidator
    {
        public static bool IsValidBaseUrl(string url, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                error = "BaseUrl is required.";
                return false;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                error = "BaseUrl must be a valid absolute URL.";
                return false;
            }

            if (!string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                error = "BaseUrl must use HTTPS.";
                return false;
            }

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                error = "BaseUrl must not contain user credentials (userinfo).";
                return false;
            }

            // Check for localhost and loopback
            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.Equals("127.0.0.1") ||
                uri.Host.Equals("::1"))
            {
                error = "BaseUrl must not point to localhost or loopback address.";
                return false;
            }

            // Try to resolve the host to IP addresses
            try
            {
                var addresses = Dns.GetHostAddresses(uri.Host);
                foreach (var addr in addresses)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork || addr.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (IsPrivateOrReservedIp(addr))
                        {
                            error = $"BaseUrl host resolves to a private or reserved IP address: {addr}";
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = $"Failed to resolve BaseUrl host: {ex.Message}";
                return false;
            }

            return true;
        }

        private static bool IsPrivateOrReservedIp(IPAddress ip)
        {
            // IPv4
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                // 10.0.0.0/8
                if (bytes[0] == 10) return true;
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                // 127.0.0.0/8 (loopback)
                if (bytes[0] == 127) return true;
                // 169.254.0.0/16 (link-local)
                if (bytes[0] == 169 && bytes[1] == 254) return true;
                // 0.0.0.0/8 (reserved)
                if (bytes[0] == 0) return true;
                // 100.64.0.0/10 (carrier-grade NAT)
                if (bytes[0] == 100 && (bytes[1] >= 64 && bytes[1] <= 127)) return true;
                // 224.0.0.0/4 (multicast)
                if (bytes[0] >= 224 && bytes[0] <= 239) return true;
                // 240.0.0.0/4 (reserved)
                if (bytes[0] >= 240) return true;
            }
            // IPv6
            else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6Multicast || ip.IsIPv6SiteLocal || ip.IsIPv6Teredo || ip.IsIPv6UniqueLocal)
                    return true;
                // ::1 (loopback)
                if (ip.Equals(IPAddress.IPv6Loopback)) return true;
            }
            return false;
        }
    }
} 