using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Rebus.Serilog;
using Serilog;
using Unipluss.Sign.Events.Client;
using Unipluss.Sign.Events.Entities;

namespace Unipluss.Sign.Downloader
{
    public class ServiceHost
    {
        private EventClient client;
        private static JsonSerializerSettings settings=new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
        };
        public void Start()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            settings.Converters.Add(new StringEnumConverter());

            if (!string.IsNullOrWhiteSpace(AppSettingsReader.APIPRIMARYKEY))
             client = EventClient.SetupWithPrimaryApiKey(AppSettingsReader.EventQueueConnectionString, AppSettingsReader.ApiID, AppSettingsReader.APIPRIMARYKEY);
            else if(!string.IsNullOrWhiteSpace(AppSettingsReader.APISECONDARYKEY))
                client = EventClient.SetupWithPrimaryApiKey(AppSettingsReader.EventQueueConnectionString, AppSettingsReader.ApiID, AppSettingsReader.APISECONDARYKEY);
            else
            {
                string msg= "API key is missing, primary or secondary key must be specified";
                Log.Logger.Error(msg);
                throw new ApplicationException(msg);
            }

            client
                .AddRebusCompatibeLogger(x=>x.Serilog(Log.Logger))
                .SubscribeToDocumentSignedEvent(
                    AppSettingsReader.MetaDataFormat==MetaDataFormat.NONE ? (Func<DocumentSignedEvent, Task>) null:DocumentSignedEvent)
                .SubscribeToDocumentCanceledEvent(
                    AppSettingsReader.MetaDataFormat == MetaDataFormat.NONE ? (Func<DocumentCanceledEvent, Task>)null : DocumentCanceledEvent)
                .SubscribeToDocumentPartiallySignedEvent(
                    AppSettingsReader.MetaDataFormat == MetaDataFormat.NONE ? (Func<DocumentPartiallySignedEvent, Task>)null : DocumentPartiallySignedEvent)
                .SubscribeToDocumentPadesSavedEvent(
                    AppSettingsReader.FilesToDownload == FilesToDownload.ALL || AppSettingsReader.FilesToDownload == FilesToDownload.PADES
                    ? DocumentPadesSavedEvent :  (Func<DocumentPadesSavedEvent, byte[], Task>) null)
                .SubscribeToDocumentSDOSavedEvent(
                    AppSettingsReader.FilesToDownload == FilesToDownload.ALL || AppSettingsReader.FilesToDownload == FilesToDownload.SDO
                    ? DocumentSDOSavedEvent : (Func<DocumentSDOSavedEvent, byte[], Task>)null)
                .Start();
        }

        private Task DocumentSDOSavedEvent(DocumentSDOSavedEvent sdoSavedEvent, byte[] sdo)
        {
            File.WriteAllBytes(Path.Combine(AppSettingsReader.DownloadPath,
                createFileName(sdoSavedEvent.DocumentId, sdoSavedEvent.ExternalDocumentId, "sdo")), sdo);
            return Task.FromResult(true);
        }

        private Task DocumentPadesSavedEvent(DocumentPadesSavedEvent @event, byte[] pades)
        {
            File.WriteAllBytes(Path.Combine(AppSettingsReader.DownloadPath,
                createFileName(@event.DocumentId,@event.ExternalDocumentId,"sdo")),pades);
            return Task.FromResult(true);
        }

        private Task DocumentPartiallySignedEvent(DocumentPartiallySignedEvent partiallySignedEvent)
        {
            SerializeEventToFile(partiallySignedEvent, partiallySignedEvent.DocumentId, partiallySignedEvent.ExternalDocumentId, "partial");
            return Task.FromResult(true);
        }

        private Task DocumentCanceledEvent(DocumentCanceledEvent canceledEvent)
        {
            SerializeEventToFile(canceledEvent, canceledEvent.DocumentId, canceledEvent.ExternalDocumentId,"cancled");
            return Task.FromResult(true);
        }

        private Task DocumentSignedEvent(DocumentSignedEvent signedEvent)
        {
           SerializeEventToFile(signedEvent,signedEvent.DocumentId,signedEvent.ExternalDocumentId);
           return Task.FromResult(true);
        }

        private static void SerializeEventToFile<T>(T @event,Guid DocumentId,string ExternalDocumentId, string postfix=null)
        {
            byte[] data = null;
            string extension=null;
            switch (AppSettingsReader.MetaDataFormat)
            {
                case MetaDataFormat.JSON:
                    extension = "json";
                    data =Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event,Formatting.Indented, settings));
                    break;
                case MetaDataFormat.XML:
                    extension = "xml";
                    XmlSerializer x = new XmlSerializer(typeof(T));
                    using (var ms = new MemoryStream())
                    {
                        x.Serialize(ms,@event);
                        data = ms.ToArray();
                    }
                    break;
            }
            if (data != null && extension != null)
                File.WriteAllBytes(Path.Combine(AppSettingsReader.DownloadPath,
                createFileName(DocumentId, ExternalDocumentId, extension, string.IsNullOrWhiteSpace(postfix)?null:postfix)), data);
                
        }

        private static string createFileName(Guid docid, string externalDocumentId,string extension, string postFix=null)
        {
            if (!string.IsNullOrWhiteSpace(postFix))
                postFix = string.Format("_{0}", postFix);
            else
            {
                postFix=String.Empty;
            }

            if (AppSettingsReader.FilenameFormat == FilenameFormat.EXTERNALID)
                return string.Format("{0}{1}.{2}", externalDocumentId, postFix, extension);

            return string.Format("{0}{1}.{2}", docid, postFix, extension);
        }

        public void Stop()
        {
            client.Dispose();
        }
    }
}
