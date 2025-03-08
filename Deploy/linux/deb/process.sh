#!/bin/bash

# Clean-up
rm -rf ./Deploy/linux/deb/out/
rm -rf ./Deploy/linux/deb/staging_folder/

# .NET publish
# self-contained is recommended, so final users won't need to install .NET
dotnet publish "./CrossPlatformDownloadManager.DesktopApp/CrossPlatformDownloadManager.DesktopApp.csproj" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime linux-x64 \
  --output "./Deploy/linux/deb/out/linux-x64"

# Create directories
mkdir -p ./Deploy/linux/deb/staging_folder/DEBIAN
mkdir -p ./Deploy/linux/deb/staging_folder/usr/bin
mkdir -p ./Deploy/linux/deb/staging_folder/usr/lib/cross-platform-download-manager
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/applications
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/16x16/apps
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/32x32/apps
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/48x48/apps
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/64x64/apps
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/128x128/apps
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/256x256/apps
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/512x512/apps
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/1024x1024/apps
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/scalable/apps
mkdir -p ./Deploy/linux/deb/staging_folder/usr/share/pixmaps

# Debian control file
cp ./Deploy/linux/deb/control ./Deploy/linux/deb/staging_folder/DEBIAN

# Debian copyright file
cp ./Deploy/linux/deb/copyright ./Deploy/linux/deb/staging_folder/DEBIAN

# Starter script
cp ./Deploy/linux/basics/starter-script.sh ./Deploy/linux/deb/staging_folder/usr/bin/cross-platform-download-manager
chmod +x ./Deploy/linux/deb/staging_folder/usr/bin/cross-platform-download-manager # set executable permissions to starter script

# Other files
cp -f -a ./Deploy/linux/deb/out/linux-x64/. ./Deploy/linux/deb/staging_folder/usr/lib/cross-platform-download-manager/ # copies all files from publish dir
chmod -R a+rwX ./Deploy/linux/deb/staging_folder/usr/lib/cross-platform-download-manager/ # set read and write permissions to all files
chmod +x ./Deploy/linux/deb/staging_folder/usr/lib/cross-platform-download-manager/CrossPlatformDownloadManager.DesktopApp # set executable permissions to main executable

# Desktop shortcut
cp ./Deploy/linux/basics/app.desktop ./Deploy/linux/deb/staging_folder/usr/share/applications/cross-platform-download-manager.desktop

# Desktop icon
# A 1024px x 1024px PNG, like VS Code uses for its icon
cp ./Deploy/linux/basics/icons/icon-1024.png ./Deploy/linux/deb/staging_folder/usr/share/pixmaps/cross-platform-download-manager.png

# Hicolor icons
cp ./Deploy/linux/basics/icons/icon-16.png ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/16x16/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-32.png ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/32x32/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-48.png ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/48x48/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-64.png ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/64x64/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-128.png ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/128x128/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-256.png ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/256x256/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-512.png ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/512x512/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-1024.png ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/1024x1024/apps/cross-platform-download-manager.png
cp ./Deploy/linux/basics/icons/icon-scalable.svg ./Deploy/linux/deb/staging_folder/usr/share/icons/hicolor/scalable/apps/cross-platform-download-manager.svg

# Ensure Deploy/bin directory exists
mkdir -p ./Deploy/bin/

# Make .deb file
dpkg-deb --root-owner-group --build ./Deploy/linux/deb/staging_folder/ ./Deploy/bin/Cross-platform.Download.Manager.linux-amd64.deb
