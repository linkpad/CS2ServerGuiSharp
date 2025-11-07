# CS2 Server GUI - ModSharp

This project is a C# port of the original C++ [CS2ServerGUI](https://github.com/Source2ZE/CS2ServerGUI/tree/master) using ImGui.NET and Silk.NET for [ModSharp](https://github.com/Kxnrl/modsharp-public).

# building
- dotnet publish
- copy dlls from `bin\Release\net9.0\publish\runtimes\win-x64\native` to `\sharp\core` folder.
- then copy everything from `bin\Release\net9.0\publish\` folder in `sharp\modules` like a normal module (with the runtimes folder too)