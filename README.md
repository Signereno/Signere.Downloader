# Signere.Downloader
### Signere.no Downloader service

This a service to download signed documents from Signere.no.

The service installs as a Windows Service and subscribes to document events.
###Features:
* Download signed document files (SDOs and PAdES PDFs) with documentid or externalid (your id) as the filename.
* Download document metadata, such as the signers' name(s), sign time, social security number etc. The metadata can be downloaded as a json or XML file with the same file name as the SDO and the PAdES PDF files and appropriate extensions (either .xml or .json).
* Download structured form data (ie. inputs from form fields), either as json or XML files.

#### To get the service running you need the following 3 things:
1. A Signere.no account (contact sales@signere.no to get this)
2. A Signere.no API ID and associated API keys (contact sales@signere.no to get this)
3. A Signere.no event queue connection string (contact support@signere.no to get this)

#### Settings in the .config file you should provide:
* API-ID (Should be your Signere account ID)
* API-KEY (Should be your Signere secondary API key)
* EventQueueConnectionString (you'll get this from Signere.no support)
* DownloadPath (Path on the local machine to download and save the files to)
* FilenameFormat (SignereDocumentId, ExternalID (your id))
* MetaDataFormat (either "xml", "json" or "none")
* FilesToDownload (either "sdo", "pades" or "all")

### [Download](https://github.com/Signereno/Signere.Downloader/raw/master/dist/Signere-Downloader.zip) or pull down the repo and compile the project for yourself.

### Install
Setup the .config file with the settings and then run the install.bat file, this will install the Signere downloader as a Windows service and start the service.

### Uninstall
Run the uninstall.bat file, this will stop and uninstall the Windows service, but not delete any files.

### Logging
The service uses Serilog for logging, and the default settings are log to file and console. For more settings see the  [Serilog wiki](https://github.com/serilog/serilog/wiki/AppSettings)
