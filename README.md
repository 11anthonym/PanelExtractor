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

Leave the output folder blank to save beside the application. For panels without SFTP, enable **Allow legacy FTP fallback (unencrypted)**.

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
