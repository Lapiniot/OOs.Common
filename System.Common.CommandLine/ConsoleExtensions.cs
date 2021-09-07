using System.Text;

namespace System.Common.CommandLine;

public static class ConsoleExtensions
{
    public static string ReadPassword(bool echoInput = true)
    {
        var sb = new StringBuilder();
        while(true)
        {
            var ch = Console.ReadKey(true);
            switch(ch.Key)
            {
                case ConsoleKey.Enter: return sb.ToString();
                case ConsoleKey.Escape: return null;
                case ConsoleKey.Backspace: if(sb.Length > 0) sb.Remove(sb.Length - 1, 1); break;
                default: sb.Append(ch.KeyChar); break;
            }

            if(echoInput)
            {
                var (_, top) = Console.GetCursorPosition();
                Console.SetCursorPosition(0, top);
                Console.Write(new String(' ', sb.Length + 1));
                Console.SetCursorPosition(0, top);
                Console.Write(new String('*', sb.Length));
            }
        }
    }
}