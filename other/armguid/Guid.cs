using System;
using System.Text;
using System.Security.Cryptography;

string ArmGuid(params string[] args)
{
    return UuidV5.Generate(
        new Guid("11fb06fb-712d-4ddd-98c7-e71bbd588830"),
        string.Join("-", args)).ToString();
}

// Examples:
Console.WriteLine(ArmGuid("myResourceGroup"));
Console.WriteLine(ArmGuid("myResourceGroup", "myStorageAccount"));

public static class UuidV5
{
    /// <summary>
    /// Generates a UUIDv5 (name-based, SHA-1) from a namespace UUID and a name string.
    /// </summary>
    /// <param name="namespaceId">Namespace UUID (e.g., Guid for DNS, URL, etc.)</param>
    /// <param name="name">Name string to hash</param>
    /// <returns>UUIDv5 as Guid</returns>
    public static Guid Generate(Guid namespaceId, string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name must not be null or empty.", nameof(name));

        // Convert namespace UUID to network byte order (big-endian)
        byte[] namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);

        // Convert name to bytes
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);

        // Concatenate namespace and name, then hash with SHA-1
        byte[] hash;
        using (SHA1 sha1 = SHA1.Create())
        {
            sha1.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
            sha1.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
            hash = sha1.Hash!;
        }

        // Most significant bits of hash become UUID
        byte[] newGuid = new byte[16];
        Array.Copy(hash, 0, newGuid, 0, 16);

        // Set version to 5 (name-based, SHA-1)
        newGuid[6] = (byte)((newGuid[6] & 0x0F) | (5 << 4));

        // Set variant to RFC 4122
        newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

        // Convert to little-endian for Guid
        SwapByteOrder(newGuid);

        return new Guid(newGuid);
    }

    // Helper: Swap byte order for RFC 4122 compliance
    private static void SwapByteOrder(byte[] guid)
    {
        void Swap(int a, int b) { byte t = guid[a]; guid[a] = guid[b]; guid[b] = t; }

        Swap(0, 3);
        Swap(1, 2);
        Swap(4, 5);
        Swap(6, 7);
    }
}