using System;
using System.Linq;

namespace Check
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("--- WebhookType ---");
            var properties = typeof(Net.payOS.Types.WebhookType).GetProperties();
            foreach (var prop in properties)
            {
                Console.WriteLine($"{prop.Name}: {prop.PropertyType.Name}");
            }
        }
    }
}
