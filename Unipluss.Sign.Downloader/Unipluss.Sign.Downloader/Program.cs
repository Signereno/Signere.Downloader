using Serilog;
using Topshelf;

namespace Unipluss.Sign.Downloader
{
    public class Program
    {
        public static void Main()
        {

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            HostFactory.Run(x =>
            {
                x.Service<ServiceHost>(s =>
                {
                    s.ConstructUsing(name => new ServiceHost());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();
                x.UseSerilog(Log.Logger);
                x.SetDescription("Signere.no downloader service");
                x.SetDisplayName("Signere.no downloader");
                x.SetServiceName("Signere.Downloader");
                x.StartAutomatically();
            });
        }
    }
}