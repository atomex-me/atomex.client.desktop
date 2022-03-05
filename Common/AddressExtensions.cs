namespace Atomex.Client.Desktop.Common
{
    public static class AddressExtensions
    {
        public static string TruncateAddress(this string address)
        {
            if (string.IsNullOrEmpty(address) || address.Length < 9)
                return address;

            return $"{address[..4]}···{address[^5..]}";
        }
    }
}