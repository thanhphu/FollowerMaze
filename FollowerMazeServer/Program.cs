using System;
using System.Diagnostics.CodeAnalysis;

namespace FollowerMazeServer
{
    [ExcludeFromCodeCoverage]
    class Program
    {
        static void Main()
        {
            EventListener L = new EventListener();

            // Write status update on screen
            System.Timers.Timer StatusTimer = new System.Timers.Timer(Constants.StatusInterval);
            StatusTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                Logger.Status($"Clients: Pending={L.PendingClientsCount} Connected={L.ConnectedClientsCount} " +
                    $"Messages: Pending={L.PendingMessagesCount} Processed={L.ProcessedMessagesCount}");
            };
            StatusTimer.Enabled = true;
            
            L.Start();
            Console.WriteLine("Press ENTER to stop listening");
            Console.ReadLine();
            L.Stop();

            StatusTimer.Dispose();
        }
    }
}
