using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unipluss.Sign.Downloader
{
    public static class AppSettingsReader
    {

        public static string DownloadPath => GetSetting("DownloadPath");
        public static Guid ApiID =>new Guid(GetSetting("API-ID"));

        public static string APIPRIMARYKEY => GetSetting("API-PRIMARYKEY");

        public static string APISECONDARYKEY => GetSetting("API-SECONDARYKEY");

        public static string EventQueueConnectionString => ConfigurationManager.ConnectionStrings["EventQueueConnectionString"].ConnectionString;



        public static FilesToDownload FilesToDownload
        {
            get
            {
                try
                {
                    var value = GetSetting("FilesToDownload");
                    FilesToDownload result=FilesToDownload.NONE;

                    Enum.TryParse(value, true, out result);
                    return result;
                }
                catch (Exception)
                {
                    return FilesToDownload.NONE;
                }
            }
        }

        public static FilenameFormat FilenameFormat
        {
            get
            {
                try
                {
                    var value = GetSetting("FilenameFormat");
                    FilenameFormat result = FilenameFormat.SIGNEREDOCUMENTID;

                    Enum.TryParse(value, true, out result);
                    return result;
                }
                catch (Exception)
                {
                    return FilenameFormat.SIGNEREDOCUMENTID;
                }
            }
        }
        public static MetaDataFormat MetaDataFormat
        {
            get
            {
                try
                {
                    var value = GetSetting("MetaDataFormat");
                    MetaDataFormat result = MetaDataFormat.NONE;

                    Enum.TryParse(value, true, out result);
                    return result;
                }
                catch (Exception)
                {
                    return MetaDataFormat.NONE;
                }
            }
        }

        private static string GetSetting(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;
            if(ConfigurationManager.AppSettings.AllKeys.Contains(key))
                return ConfigurationManager.AppSettings[key];
            else if (ConfigurationManager.AppSettings.AllKeys.Contains(key.ToLowerInvariant()))
            {
                return ConfigurationManager.AppSettings[key.ToLowerInvariant()];
            }
            else if (ConfigurationManager.AppSettings.AllKeys.Contains(key.ToUpperInvariant()))
            {
                return ConfigurationManager.AppSettings[key.ToUpperInvariant()];
            }

            return null;
        }
    }

    public enum FilesToDownload
    {
        NONE,
        ALL,
        PADES,
        SDO,
    }

    public enum MetaDataFormat
    {
        NONE,
        XML,
        JSON,
    }

    public enum FilenameFormat
    {
        SIGNEREDOCUMENTID,
        EXTERNALID

    }
}
