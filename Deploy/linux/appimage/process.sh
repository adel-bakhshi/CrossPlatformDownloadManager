#!/bin/bash

# Check prerequisites
echo "Checking prerequisites..."
echo "Checking appimagetool..."
if [ ! -f "./appimagetool-x86_64.AppImage" ]; then
    echo "Error: appimagetool-x86_64.AppImage not found."
    exit 1
fi

echo "Checking dotnet command..."
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not installed."
    exit 1
fi

# Clean-up
echo "Cleaning up previous data..."
rm -rf ./Deploy/linux/appimage/out/ ./Deploy/linux/appimage/AppDir/ ./Deploy/bin/Cross-platform.Download.Manager.x86_64.AppImage

# .NET publish
# self-contained is recommended, so final users won't need to install .NET
echo "Publishing project..."
dotnet publish "./CrossPlatformDownloadManager.DesktopApp/CrossPlatformDownloadManager.DesktopApp.csproj" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  -p:PublishSingleFile=true \
  --runtime linux-x64 \
  --output "./Deploy/linux/appimage/out/linux-x64" || {
    echo "Publish failed";
    exit 1;
}

# Create AppDir structure
echo "Creating AppDir structure..."
mkdir -p ./Deploy/linux/appimage/AppDir/usr/lib/adel.bakhshi.cdm
mkdir -p ./Deploy/linux/appimage/AppDir/usr/share/applications
mkdir -p ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/{16x16,32x32,48x48,64x64,128x128,256x256,512x512,1024x1024,scalable}/apps
mkdir -p ./Deploy/linux/appimage/AppDir/usr/share/pixmaps
mkdir -p ./Deploy/linux/appimage/AppDir/usr/share/metainfo

# AppRun file
echo "Preparing AppRun file..."
cp ./Deploy/linux/appimage/app-run.sh ./Deploy/linux/appimage/AppDir/AppRun
chmod +x ./Deploy/linux/appimage/AppDir/AppRun # set executable permissions to AppRun

# App binaries
echo "Preparing app binaries..."
cp -f -a ./Deploy/linux/appimage/out/linux-x64/. ./Deploy/linux/appimage/AppDir/usr/lib/adel.bakhshi.cdm/ # copies all files from publish dir
chmod -R a+rwX ./Deploy/linux/appimage/AppDir/usr/lib/adel.bakhshi.cdm/ # set read and write permissions to all files
chmod +x ./Deploy/linux/appimage/AppDir/usr/lib/adel.bakhshi.cdm/CrossPlatformDownloadManager.DesktopApp # set executable permissions to main executable

# Desktop shortcut
echo "Preparing desktop file..."
cp ./Deploy/linux/appimage/app.desktop ./Deploy/linux/appimage/AppDir/adel.bakhshi.cdm.desktop
cp ./Deploy/linux/appimage/app.desktop ./Deploy/linux/appimage/AppDir/usr/share/applications/adel.bakhshi.cdm.desktop

# Desktop icon
# A 1024px x 1024px PNG, like VS Code uses for its icon
echo "Preparing desktop icon..."
cp ./Deploy/linux/basics/icons/icon-1024.png ./Deploy/linux/appimage/AppDir/adel.bakhshi.cdm.png
cp ./Deploy/linux/basics/icons/icon-1024.png ./Deploy/linux/appimage/AppDir/usr/share/pixmaps/adel.bakhshi.cdm.png

# Hicolor icons
echo "Preparing hicolor icons..."
cp ./Deploy/linux/basics/icons/icon-16.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/16x16/apps/adel.bakhshi.cdm.png
cp ./Deploy/linux/basics/icons/icon-32.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/32x32/apps/adel.bakhshi.cdm.png
cp ./Deploy/linux/basics/icons/icon-48.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/48x48/apps/adel.bakhshi.cdm.png
cp ./Deploy/linux/basics/icons/icon-64.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/64x64/apps/adel.bakhshi.cdm.png
cp ./Deploy/linux/basics/icons/icon-128.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/128x128/apps/adel.bakhshi.cdm.png
cp ./Deploy/linux/basics/icons/icon-256.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/256x256/apps/adel.bakhshi.cdm.png
cp ./Deploy/linux/basics/icons/icon-512.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/512x512/apps/adel.bakhshi.cdm.png
cp ./Deploy/linux/basics/icons/icon-1024.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/1024x1024/apps/adel.bakhshi.cdm.png
cp ./Deploy/linux/basics/icons/icon-scalable.svg ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/scalable/apps/adel.bakhshi.cdm.svg

# Create metainfo file
echo "Preparing metainfo file..."
cp ./Deploy/linux/appimage/app.appdata.xml ./Deploy/linux/appimage/AppDir/usr/share/metainfo/adel.bakhshi.cdm.appdata.xml

# Create output directory
echo "Creating output directory..."
mkdir -p ./Deploy/bin

# Create AppImage
echo "Creating AppImage file..."
COMPRESSION=zstd ./appimagetool-x86_64.AppImage ./Deploy/linux/appimage/AppDir/ ./Deploy/bin/Cross-platform.Download.Manager.x86_64.AppImage || {
    echo "AppImage creation failed";
    exit 1;
}

echo "Done!"