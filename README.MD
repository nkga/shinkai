# Shinkai

![Subnautica Multiplayer](https://raw.githubusercontent.com/nkga/shinkai/master/doc/img/subnautica_players.jpg)

Shinkai was a co-op multiplayer mod for [Subnautica](https://store.steampowered.com/app/264710/Subnautica/) based on the [Nitrox](https://github.com/SubnauticaNitrox/Nitrox) project.

For technical details on the project, there is an accompanying
[blog post](https://nkga.github.io/post/shinkai-a-subnautica-multiplayer-mod/) for this repository.

### Building

1. Install [Visual Studio Community](https://www.visualstudio.com/vs/community/).
2. Open `Shinkai.sln`.
3. Change solution configuration to `Release`.
4. Build solution.
5. Copy build files into your `Subnautica\Subnautica_Data\Managed` folder.

### Modifying the Game

1. Download [DNSpy](https://github.com/0xd4d/dnSpy).
2. Open `Assembly-CSharp.dll` in DNSpy.
3. Navigate to `GameInput` > `Awake`.
4. Right click the source, and select `Edit Method (C#)...`.
5. In the Edit Code window, press `Ctrl+O` or the folder icon to `Add Assembly Reference`.
6. Add a reference to `ShinkaiClient.dll`.
7. At the top of the source code for `Awake`, add `ShinkaiClient.Main.Execute();`.
8. Press `Compile`.
9. From the `File` menu, select `Save Module` and overwrite the old
`Assembly-CSharp.dll`.

### Running the Server

1. Ensure you have port `33322` open.
2. Run `ShinkaiServer.exe`.

### Console Commands

- `mpsave` - Requests the server to save current world state.
- `mpsync` - Reconnects and resynchronizes state with the server.
- `mpdump` - Logs objects in the world to the `output_log.txt`
