@echo off
chcp 65001 > nul

set PROTOC=protoc.exe
set PROTO_DIR=./proto
set OUT_DIR=../Assets/Scripts/Protobuf

echo =========================
echo Generate protobuf C# files
echo =========================

if not exist %OUT_DIR% (
    mkdir %OUT_DIR%
)

echo.
echo Start generate...

for %%f in (%PROTO_DIR%\*.proto) do (
    echo Generating %%f

    %PROTOC% ^
    --proto_path=%PROTO_DIR% ^
    --csharp_out=%OUT_DIR% ^
    %%f
)

echo.
echo Copy cs files...

echo.
echo Generate finished!
pause