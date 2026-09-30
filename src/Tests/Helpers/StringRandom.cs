namespace Helpers;

public static class StringRandom
{
    public static string GetRandomString(int length)
    {
        Random r = new Random();

        const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@$?_-";
        char[] chars = new char[length];

        for (int i = 0; i < length; i++)
        {
            chars[i] = allowedChars[r.Next(0, allowedChars.Length)];
        }

        return new string(chars);
    }

    public static string GetRandomAlphabeticString(int length)
    {
        Random r = new Random();

        const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        char[] chars = new char[length];

        for (int i = 0; i < length; i++)
        {
            chars[i] = allowedChars[r.Next(0, allowedChars.Length)];
        }

        return new string(chars);
    }
}
