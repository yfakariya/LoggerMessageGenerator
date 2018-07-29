dotnet test %APPVEYOR_BUILD_FOLDER%/test/Wisteria.LoggerMessageGenerator.UnitTest/Wisteria.LoggerMessageGenerator.UnitTest.csproj
if not %errorlevel% == 0 exit /b 1
