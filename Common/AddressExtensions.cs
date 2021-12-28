namespace Atomex.Client.Desktop.Common
{
    public static class AddressExtensions
    {
        public static string TruncateAddress(this string address)
        {
            if (string.IsNullOrEmpty(address))
                return address;

            return address[..4] + "···" + address[^5..];
        }
    }
}