﻿using System;
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
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        private EventClient client;

        public void Start()
        {
            
            settings.Converters.Add(new StringEnumConverter());

            if (!string.IsNullOrWhiteSpace(AppSettingsReader.APIPRIMARYKEY))
                client = EventClient.SetupWithPrimaryApiKey(AppSettingsReader.EventQueueConnectionString,
                    AppSettingsReader.ApiID, AppSettingsReader.APIPRIMARYKEY);
            else if (!string.IsNullOrWhiteSpace(AppSettingsReader.APISECONDARYKEY))
                client = EventClient.SetupWithPrimaryApiKey(AppSettingsReader.EventQueueConnectionString,
                    AppSettingsReader.ApiID, AppSettingsReader.APISECONDARYKEY);
            else
            {
                var msg = "API key is missing, primary or secondary key must be specified";
                Log.Logger.Error(msg);
                throw new ApplicationException(msg);
            }

            client
                .UseTestEnvironment(AppSettingsReader.UseSignereTestEnvironment)
                .AddRebusCompatibeLogger(x => x.Serilog(Log.Logger))
                .SubscribeToDocumentSignedEvent(
                    AppSettingsReader.MetaDataFormat == MetaDataFormat.NONE
                        ? (Func<DocumentSignedEvent, Task>) null
                        : DocumentSignedEvent)
                .SubscribeToDocumentCanceledEvent(
                    AppSettingsReader.MetaDataFormat == MetaDataFormat.NONE
                        ? (Func<DocumentCanceledEvent, Task>) null
                        : DocumentCanceledEvent)
                .SubscribeToDocumentPartiallySignedEvent(
                    AppSettingsReader.MetaDataFormat == MetaDataFormat.NONE
                        ? (Func<DocumentPartiallySignedEvent, Task>) null
                        : DocumentPartiallySignedEvent)
                .SubscribeToDocumentPadesSavedEvent(
                    AppSettingsReader.FilesToDownload == FilesToDownload.ALL ||
                    AppSettingsReader.FilesToDownload == FilesToDownload.PADES
                        ? DocumentPadesSavedEvent
                        : (Func<DocumentPadesSavedEvent, byte[], Task>) null)
                .SubscribeToDocumentSDOSavedEvent(
                    AppSettingsReader.FilesToDownload == FilesToDownload.ALL ||
                    AppSettingsReader.FilesToDownload == FilesToDownload.SDO
                        ? DocumentSDOSavedEvent
                        : (Func<DocumentSDOSavedEvent, byte[], Task>) null)
                .SubscribeToDocumentFormPartiallySignedEvent(AppSettingsReader.FilesToDownload == FilesToDownload.ALL
                    ? DocumentFormPartiallySignedEvent
                    : (Func<DocumentFormPartiallySignedEvent, Task>) null)
                .SubscribeToDocumentFormSignedEvent(AppSettingsReader.FilesToDownload == FilesToDownload.ALL
                    ? DocumentFormSignedEvent
                    : (Func<DocumentFormSignedEvent, Task>) null)
                .Start();
        }

        private Task DocumentFormPartiallySignedEvent(DocumentFormPartiallySignedEvent formPartiallySignedEvent)
        {
            SerializeEventToFile(formPartiallySignedEvent, formPartiallySignedEvent.DocumentId, "",string.Format("{0}\\{1}","Forms",toSafeFolderName(formPartiallySignedEvent.Schema)), "formData", "partial");
            return Task.FromResult(true);
        }

        private Task DocumentFormSignedEvent(DocumentFormSignedEvent formSignedEvent)
        {
            SerializeEventToFile(formSignedEvent,formSignedEvent.DocumentId,"", string.Format("{0}\\{1}", "Forms", toSafeFolderName(formSignedEvent.Schema)), "formdata","complete");
            return Task.FromResult(true);
        }

        private Task DocumentSDOSavedEvent(DocumentSDOSavedEvent sdoSavedEvent, byte[] sdo)
        {
            File.WriteAllBytes(Path.Combine(AppSettingsReader.DownloadPath,
                createFileName(sdoSavedEvent.DocumentId, sdoSavedEvent.ExternalDocumentId,"sdo")), sdo);
            return Task.FromResult(true);
        }

        private Task DocumentPadesSavedEvent(DocumentPadesSavedEvent @event, byte[] pades)
        {
            File.WriteAllBytes(Path.Combine(AppSettingsReader.DownloadPath,
                createFileName(@event.DocumentId, @event.ExternalDocumentId, "pdf","","_signerepades")), pades);
            return Task.FromResult(true);
        }

        private Task DocumentPartiallySignedEvent(DocumentPartiallySignedEvent partiallySignedEvent)
        {
            SerializeEventToFile(partiallySignedEvent, partiallySignedEvent.DocumentId,
                partiallySignedEvent.ExternalDocumentId,"","", "partial");
            return Task.FromResult(true);
        }

        private Task DocumentCanceledEvent(DocumentCanceledEvent canceledEvent)
        {
            SerializeEventToFile(canceledEvent, canceledEvent.DocumentId, canceledEvent.ExternalDocumentId,"","","canceled");
            return Task.FromResult(true);
        }

        private Task DocumentSignedEvent(DocumentSignedEvent signedEvent)
        {
            SerializeEventToFile(signedEvent, signedEvent.DocumentId, signedEvent.ExternalDocumentId);
            return Task.FromResult(true);
        }

        private static void SerializeEventToFile<T>(T @event, Guid DocumentId, string ExternalDocumentId,string subFolder=null, string preFix = null, string postfix = null)
        {
            byte[] data = null;
            string extension = null;
            switch (AppSettingsReader.MetaDataFormat)
            {
                case MetaDataFormat.JSON:
                    extension = "json";
                    data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, Formatting.Indented, settings));
                    break;
                case MetaDataFormat.XML:
                    extension = "xml";
                    var x = new XmlSerializer(typeof (T));
                    using (var ms = new MemoryStream())
                    {
                        x.Serialize(ms, @event);
                        data = ms.ToArray();
                    }
                    break;
            }
            if (data != null && extension != null)
            {
                string filepath = "";
                if (subFolder != null)
                {
                    string folderPath = Path.Combine(AppSettingsReader.DownloadPath, subFolder);
                    Directory.CreateDirectory(folderPath);
                    filepath = Path.Combine(folderPath,
                        createFileName(DocumentId, ExternalDocumentId, extension,
                            string.IsNullOrWhiteSpace(preFix) ? null : preFix, string.IsNullOrWhiteSpace(postfix)
                                ? null
                                : postfix
                        )
                    );
                }
                else
                {
                    filepath = Path.Combine(AppSettingsReader.DownloadPath,
                        createFileName(DocumentId, ExternalDocumentId, extension, string.IsNullOrWhiteSpace(postfix) ?
                        null : postfix
                        )
                    );
                }
                if (File.Exists(filepath))
                {
                    for (int i = 1; i < 10; i++)
                    {
                        filepath=filepath.Replace(string.Format( ".{0}",extension), string.Format("{1}.{0}", extension,i));
                        if(!File.Exists(filepath))
                            break;
                    }
                }

                File.WriteAllBytes(filepath, data);
            }
               
        }

        private static string createFileName(Guid docid, string externalDocumentId, string extension, string preFix=null, string postFix = null)
        {
            if (!string.IsNullOrWhiteSpace(preFix))
            {
                preFix = string.Format("{0}_", preFix);
            }
            else
            {
                preFix = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(postFix))
                postFix = string.Format("_{0}", postFix);
            else
            {
                postFix = string.Empty;
            }

            return string.Format("{0}{1}{2}.{3}", preFix, AppSettingsReader.FilenameFormat == FilenameFormat.EXTERNALID
                ? externalDocumentId
                : docid.ToString(), postFix, extension);
        }


        private static string toSafeFolderName(string subFolder)
        {
            return subFolder
                .Replace("\\", "")
                .Replace("/", "")
                .Replace("\"", "")
                .Replace("*", "")
                .Replace(":", "")
                .Replace("?", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("|", "")
                .Replace(" ", "_");
        }


        public void Stop()
        {
            client.Dispose();
        }
    }
}