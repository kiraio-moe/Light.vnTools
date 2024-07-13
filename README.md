# Light.vnTools

## Overview

Light.vnTools is an unpack/decrypt and repack/encrypt tool for game made with [Light.vn](https://lightvn.net "Light.vn") (Visual Novel) game engine.

## Requirements

- [.NET 7.0 SDK](https://dotnet.microsoft.com/download/dotnet/7.0 "Download .NET 7.0 SDK") / [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0 "Download .NET 8.0 SDK")

## Usage

> [!IMPORTANT]  
> The "new" encryption scheme that works on the newer Light.vn version is stripping the original file name,
> so I can't recover it.
>
> We're missing the original file extension, but we can guess it by reading the file header and set the extension based on that,
>
> For the time being you need to do it yourself by grabbing a hex editor program like [ImHex](https://github.com/WerWolv/ImHex "Visit ImHex GitHub repository"), find the **magic header**, do some Googling then rename the file.

- Download Light.vnTools at [Releases](https://github.com/kiraio-moe/Light.vnTools/releases "Light.vnTools Releases").
- Unpack/Decrypt:  
  Drag and drop `.vndat` / `.mcdat` file(s) to the `LightvnTools.exe`.
- Repack/Encrypt:  
  Drag and drop **unpacked folder/file(s)** to the `LightvnTools.exe`.

## Credits

Thanks to [takase1121](https://github.com/takase1121 "Visit takase1121 GitHub profile") by providing proof of concept at: <https://github.com/morkt/GARbro/issues/440>

## License

This project is licensed under GNU GPL 3.0.

For more information about the GNU General Public License version 3.0 (GNU GPL 3.0), please refer to the official GNU website: <https://www.gnu.org/licenses/gpl-3.0.html>

## Disclaimer

This tool is intentionally created as a modding tool to translate the visual novel game created with Light.vn game engine. I didn't condone any piracy in any forms such as taking the game visual and sell it illegally which harm the developer. Use the tool at your own risk (DWYOR - Do What You Own Risk).
