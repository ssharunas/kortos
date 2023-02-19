#!/bin/sh

dotnet publish --sc  -r linux-arm
echo uploading...
rsync -aurv ./bin/Debug/net7.0/linux-arm/publish/ pi@torontas:/srv/cshtml/kortos/

