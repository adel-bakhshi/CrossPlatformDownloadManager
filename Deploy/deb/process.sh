#!/bin/bash

# Clean-up
rm -rf ./out/
rm -rf ./staging_folder/

# .NET publish
# self-contained is recommended, so final users won't need to install .NET
dotnet publish "../../CrossPlatformDownloadManager.DesktopApp/CrossPlatformDownloadManager.DesktopApp.csproj" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime linux-x64 \
  --output "../Deploy/deb/out/linux-x64"

# Create directories
mkdir -p staging_folder/DEBIAN
mkdir -p staging_folder/usr/bin
mkdir -p staging_folder/usr/lib/cross-platform-download-manager
mkdir -p staging_folder/usr/share/applications
mkdir -p staging_folder/usr/share/icons/hicolor/16x16/apps
mkdir -p staging_folder/usr/share/icons/hicolor/32x32/apps
mkdir -p staging_folder/usr/share/icons/hicolor/48x48/apps
mkdir -p staging_folder/usr/share/icons/hicolor/64x64/apps
mkdir -p staging_folder/usr/share/icons/hicolor/128x128/apps
mkdir -p staging_folder/usr/share/icons/hicolor/256x256/apps
mkdir -p staging_folder/usr/share/icons/hicolor/512x512/apps
mkdir -p staging_folder/usr/share/icons/hicolor/1024x1024/apps
mkdir -p staging_folder/usr/share/icons/hicolor/scalable/apps
mkdir -p staging_folder/usr/share/pixmaps

# Debian control file
cp ./control ./staging_folder/DEBIAN

# Debian copyright file
cp ./copyright ./staging_folder/DEBIAN

# Starter script
cp ./starter-script.sh ./staging_folder/usr/bin/cross-platform-download-manager
chmod +x ./staging_folder/usr/bin/cross-platform-download-manager # set executable permissions to starter script

# Other files
cp -f -a ./out/linux-x64/. ./staging_folder/usr/lib/cross-platform-download-manager/ # copies all files from publish dir
chmod -R a+rwX ./staging_folder/usr/lib/cross-platform-download-manager/ # set read and write permissions to all files
chmod +x ./staging_folder/usr/lib/cross-platform-download-manager/CrossPlatformDownloadManager.DesktopApp # set executable permissions to main executable

# Desktop shortcut
cp ./app.desktop ./staging_folder/usr/share/applications/cross-platform-download-manager.desktop

# Desktop icon
# A 1024px x 1024px PNG, like VS Code uses for its icon
cp ./icons/icon-1024.png ./staging_folder/usr/share/pixmaps/cross-platform-download-manager.png

# Hicolor icons
cp ./icons/icon-16.png ./staging_folder/usr/share/icons/hicolor/16x16/apps/cross-platform-download-manager.png
cp ./icons/icon-32.png ./staging_folder/usr/share/icons/hicolor/32x32/apps/cross-platform-download-manager.png
cp ./icons/icon-48.png ./staging_folder/usr/share/icons/hicolor/48x48/apps/cross-platform-download-manager.png
cp ./icons/icon-64.png ./staging_folder/usr/share/icons/hicolor/64x64/apps/cross-platform-download-manager.png
cp ./icons/icon-128.png ./staging_folder/usr/share/icons/hicolor/128x128/apps/cross-platform-download-manager.png
cp ./icons/icon-256.png ./staging_folder/usr/share/icons/hicolor/256x256/apps/cross-platform-download-manager.png
cp ./icons/icon-512.png ./staging_folder/usr/share/icons/hicolor/512x512/apps/cross-platform-download-manager.png
cp ./icons/icon-1024.png ./staging_folder/usr/share/icons/hicolor/1024x1024/apps/cross-platform-download-manager.png
cp ./icons/cdm-logo.svg ./staging_folder/usr/share/icons/hicolor/scalable/apps/cross-platform-download-manager.svg

# Ensure Deploy/bin directory exists
mkdir -p ../bin

# Make .deb file
dpkg-deb --root-owner-group --build ./staging_folder/ ../bin/Cross-platform.Download.Manager.linux-amd64.deb

# Clean-up
rm -rf ./out/
rm -rf ./staging_folder/
