#!/bin/bash

# Check prerequisites
if [ ! -f "./appimagetool-x86_64.AppImage" ]; then
    echo "Error: appimagetool-x86_64.AppImage not found."
    exit 1
fi

if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not installed."
    exit 1
fi

# Clean-up
rm -rf ./Deploy/linux/appimage/out/ ./Deploy/linux/appimage/AppDir/ ./Deploy/bin/Cross-platform.Download.Manager.x86_64.AppImage

# .NET publish
# self-contained is recommended, so final users won't need to install .NET
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
mkdir -p ./Deploy/linux/appimage/AppDir/usr/lib/cross-platform-download-manager
mkdir -p ./Deploy/linux/appimage/AppDir/usr/share/applications
mkdir -p ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/{16x16,32x32,48x48,64x64,128x128,256x256,512x512,1024x1024,scalable}/apps
mkdir -p ./Deploy/linux/appimage/AppDir/usr/share/pixmaps

# AppRun file
cp ./Deploy/linux/appimage/app-run.sh ./Deploy/linux/appimage/AppDir/AppRun
chmod +x ./Deploy/linux/appimage/AppDir/AppRun # set executable permissions to AppRun

# App binaries
cp -f -a ./Deploy/linux/appimage/out/linux-x64/. ./Deploy/linux/appimage/AppDir/usr/lib/cross-platform-download-manager/ # copies all files from publish dir
chmod -R a+rwX ./Deploy/linux/appimage/AppDir/usr/lib/cross-platform-download-manager/ # set read and write permissions to all files
chmod +x ./Deploy/linux/appimage/AppDir/usr/lib/cross-platform-download-manager/CrossPlatformDownloadManager.DesktopApp # set executable permissions to main executable

# Desktop shortcut
cp ./Deploy/linux/basics/app.desktop ./Deploy/linux/appimage/AppDir/cross-platform-download-manager.desktop
cp ./Deploy/linux/basics/app.desktop ./Deploy/linux/appimage/AppDir/usr/share/applications/cross-platform-download-manager.desktop

# Desktop icon
# A 1024px x 1024px PNG, like VS Code uses for its icon
cp ./Deploy/linux/basics/icons/icon-1024.png ./Deploy/linux/appimage/AppDir/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-1024.png ./Deploy/linux/appimage/AppDir/usr/share/pixmaps/cross-platform-download-manager.png

# Hicolor icons
cp ./Deploy/linux/basics/icons/icon-16.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/16x16/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-32.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/32x32/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-48.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/48x48/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-64.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/64x64/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-128.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/128x128/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-256.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/256x256/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-512.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/512x512/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-1024.png ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/1024x1024/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-scalable.svg ./Deploy/linux/appimage/AppDir/usr/share/icons/hicolor/scalable/apps/cross-platform-download-manager.svg

# Create output directory
mkdir -p ./Deploy/bin

# Create AppImage
COMPRESSION=zstd ./appimagetool-x86_64.AppImage ./Deploy/linux/appimage/AppDir/ ./Deploy/bin/Cross-platform.Download.Manager.x86_64.AppImage || {
    echo "AppImage creation failed";
    exit 1;
}

echo "Done!"
