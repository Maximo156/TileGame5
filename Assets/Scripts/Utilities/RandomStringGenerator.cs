using System;
using System.Text;
using System.Linq;

public static class RandomStringGenerator
{
    private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private static Random random = new Random(); // Use a single static Random instance

    public static string GenerateRandomString(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");

        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = AllowedChars[random.Next(AllowedChars.Length)];
        }
        return new string(chars);
    }
}

