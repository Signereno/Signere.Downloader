using System;
using Topshelf;

namespace Unipluss.Sign.Downloader
{
    public class Program
    {
        public static void Main()
        {
            HostFactory.Run(x =>                                 //1
            {
                x.Service<ServiceHost>(s =>                        //2
                {
                    s.ConstructUsing(name => new ServiceHost());     //3
                    s.WhenStarted(tc => tc.Start());              //4
                    s.WhenStopped(tc => tc.Stop());               //5
                });
                x.RunAsLocalSystem();                            //6
       
                x.SetDescription("Signere.no downloader service");        //7
                x.SetDisplayName("Signere.no downloader");                       //8
                x.SetServiceName("Signere.Downloader");      
                //9
            });                                                  //10
        }
    }
}