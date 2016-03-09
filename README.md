# Signere.Downloader
### Signere.no Downloader service

This a service to download signed documents from Signere.no.

The service installs as a Windows Service and subscribes to document events.
###Features:
* Download signed document files (SDOs and PADES PDFs) with documentid or externalid (your id) as the filename.
* Download document metadata (the signersname, signtime, socialsecuritynumber etc.) the metadata can be downloaded as a json or xml file with the same filename as the SDO and the PADES pdf with extensions (.xml or .json).

#### To get the service running you need the following 3 things:
1. Signere.no account  (contact sales@signere.no to get)
2. Signere.no API-id and API key (contact sales@signere.no to get)
3. Signere.no eventqueue connectionstring (contact support@signere.no to get)