# Panel Extractor

<img src="Assets/PanelExtractor.png" alt="Panel Extractor icon" width="128">

Panel Extractor downloads a deployed project from compatible Crestron® TSW touch panels and saves it as a `.vtz` archive.

## Requirements

- 64-bit Windows 10, Windows 11, or Windows Server 2016 or later with Desktop Experience
- Network access and valid panel credentials

The release includes the required .NET runtime.

## Usage

1. Extract the release ZIP and open `PanelExtractor.exe`.
2. Enter the panel address and credentials.
3. Choose an output folder.
4. Select **Test Connection**, then **Extract VTZ**.
5. Optionally open the finished archive in Crestron XPanel when prompted.

Leave the output folder blank to save beside the application.

## Panel access

- Sign in with an account in the panel's **Administrators** group. Lower access levels authenticate successfully but cannot read the project folder.
- A panel with authentication turned off is reached with its default credentials. A panel that still takes a blank password has never had an account created, so it has no project to extract.
- Panels lock the account and block the connecting computer's IP address after a few failed attempts. Check credentials rather than retrying.
- SFTP is always tried first. **Allow legacy FTP fallback (unencrypted)** only helps an older panel that has authentication turned off: turning authentication on disables the panel's FTP server, and the newest panels do not run one at all.
- Panels that require a second authentication factor cannot be read by this application.

## Opening the archive

Crestron XPanel is the only application that reads a `.vtz`, and Windows registers no handler for the extension, so the archive is offered to XPanel directly rather than to the shell. XPanel is looked for under both program folders and at whatever location its installer registered.

If XPanel is not installed the archive is still saved, and a link to the [XPanel Desktop installer](https://www.crestron.com/Resources/XPanel-Desktop-Installer) is shown.

Panel access is read-only: the application does not upload, delete, rename, or run remote commands.

Only use it on systems you own or manage.

## Development

Requires the .NET 10 SDK. Release packaging also needs the .NET 8 runtime for the SBOM tool.

```powershell
dotnet test PanelExtractor.sln --configuration Release --filter "TestCategory!=Integration"
.\scripts\Build-Release.ps1
```

## License

Copyright © 2026 Anthony Moretti. Licensed under the [MIT License](LICENSE). Runtime dependency notices are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

Crestron® is a registered trademark of Crestron Electronics, Inc. Panel Extractor is not affiliated with or endorsed by Crestron Electronics, Inc.
