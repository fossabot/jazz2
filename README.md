# ![Jazz² Resurrection](https://github.com/deathkiller/jazz2/raw/master/Docs/Logo.gif)
Jazz² Resurrection is reimplementation of game ***Jazz Jackrabbit 2*** from year 1998. Supports various versions of the game (Shareware Demo, Holiday Hare '98, The Secret Files and Christmas Chronicles). Also, it partially supports some features of [JJ2+](http://jj2.plus/) extension.


## Dependencies
### Windows
* .NET Framework 4.5.2 (or newer)
* [OpenALSoft](https://github.com/opentk/opentk-dependencies)
  * Copy `x86/openal32.dll` to `‹Game›/Extensions/OpenALSoft.x86.dll`
  * Copy `x64/openal32.dll` to `‹Game›/Extensions/OpenALSoft.x64.dll`
* [libopenmpt](http://lib.openmpt.org/libopenmpt/)
  * Copy `libopenmpt.dll` (*x86*, and its dependencies) to `‹Game›` directory

### Linux
* [Mono 4.6 (or newer)](http://www.mono-project.com/download/#download-lin)
* OpenAL
* [libopenmpt](http://lib.openmpt.org/libopenmpt/)
  * Copy `libopenmpt.so` (*x86*, and its dependencies) to `‹Game›` directory

### macOS
* [Mono 4.6 (or newer)](http://www.mono-project.com/download/#download-mac)

### Android
* Xamarin
* [libopenmpt](http://lib.openmpt.org/libopenmpt/) (included for *armeabi-v7a* and *x86*)

Requires [Microsoft Visual Studio 2017](https://www.visualstudio.com/) (or equivalent Mono compiler) to build the solution.


## Running the application
### Windows / Linux / macOS
* Build the solution
* Copy `Content` directory to `‹Game›/Content`
* Run `‹Game›/Import.exe "Path to JJ2"` (or drag and drop JJ2 directory on `Import.exe`)
  * On macOS, you can run `mono Import.exe "Path to JJ2"`
* Run `‹Game›/Jazz2.exe`
  * On macOS, you can run `mono Jazz2.exe`

*You can run `Import.exe` without parameters to show additional options.*

### Android
* Build the solution
* Run `‹Game›/Import.exe "Path to JJ2"` (or drag and drop JJ2 directory on `Import.exe`)
* Copy `‹Game›/Content` directory to `‹SDCard›/Jazz2.Android/Content` 
* Copy files from `Jazz2.Android/Shaders` directory to `‹SDCard›/Jazz2.Android/Content/Shaders` 
* Copy files from `Jazz2.Android/Shaders/Internal` directory to `‹SDCard›/Jazz2.Android/Content/Internal`
* *Create empty file `.nomedia` in `‹SDCard›/Jazz2.Android` to hide game files in Android Gallery (optimal)*
* Install APK file on Android
* Run the application

*Requires device with Android 4.4 (or newer) and OpenGL ES 3.0. `‹SDCard›` could be internal or external storage. The application tries to autodetect correct path. Also, you can use `‹SDCard›/Android/Data/Jazz2.Android` or `‹SDCard›/Download/Jazz2.Android` instead.*


## Building the solution
### Windows
* Open the solution in [Microsoft Visual Studio 2017](https://www.visualstudio.com/) and build it
* Copy `/Packages/AdamsLair.OpenTK.x.y.z/lib/OpenTK.dll.config` to `/Jazz2/Bin/Debug/OpenTK.dll.config`
* Copy dependencies to `/Jazz2/Bin/Debug/` or `/Jazz2/Bin/Release/`

### Linux
* Install [Mono 5.0 (or newer)](http://www.mono-project.com/download/#download-lin)
* Run following commands in directory with the solution file (.sln):
```bash
sudo apt install nuget
nuget restore
msbuild
```
* Copy `/Packages/AdamsLair.OpenTK.x.y.z/lib/OpenTK.dll.config` to `/Jazz2/Bin/Debug/OpenTK.dll.config`
* Obtain and copy `libopenmpt.so` to `/Jazz2/Bin/Debug/libopenmpt.so` to enable music playback
* Then you can rebuild the solution only with `msbuild` command
* Use `msbuild /p:Configuration=Release` to build Release configuration, you have to replace `Debug` with `Release` in paths

### macOS
* Install [Mono 5.0 (or newer)](http://www.mono-project.com/download/#download-mac)
* Open the solution in [Microsoft Visual Studio for Mac](https://www.visualstudio.com/vs/visual-studio-mac/) and build it
* Copy `/Packages/AdamsLair.OpenTK.x.y.z/lib/OpenTK.dll.config` to `/Jazz2/Bin/Debug/OpenTK.dll.config`

*Errors about Jazz2.Android project can be ignored, if you don't need Android build.*


## License
This software is licensed under the [GNU General Public License v3.0](./LICENSE).