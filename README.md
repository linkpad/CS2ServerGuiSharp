# CS2 Server GUI - C# port

This project is a C# port of the original C++ [CS2ServerGUI](https://github.com/Source2ZE/CS2ServerGUI/tree/master) using ImGui.NET and Silk.NET.

# building
- dotnet publish
- then you have to copy dlls from `bin\Release\net9.0\publish\runtimes\win-x64\native` and add it into `\sharp\core` folder.
- for the others dlls not inside the `runtimes` folder, copy it in `sharp\modules` like a normal module