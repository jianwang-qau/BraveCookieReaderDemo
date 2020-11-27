using System;

namespace BraveBrowserCookieReaderDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the hostname and press enter:");
            var hostname = Console.ReadLine();
            var cookieReader = new BraveCookieReader();
            var cookies = cookieReader.ReadCookies(hostname);

            foreach (var cookie in cookies)
            {
                Console.WriteLine($"Cookie Name: {cookie.Item1}");
                Console.WriteLine($"Cookie Name: {cookie.Item2}");
            }
        }
    }
}
