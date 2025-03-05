#!/bin/bash

# Clean-up previous builds
rm -rf ./out/
rm -rf ./Cross-platform.Download.Manager.linux-amd64.tar.gz

# .NET publish
# self-contained is recommended, so final users won't need to install .NET
dotnet publish "../../CrossPlatformDownloadManager.DesktopApp/CrossPlatformDownloadManager.DesktopApp.csproj" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime linux-x64 \
  --output "../Deploy/tarball/out/linux-x64"

# Check if publish was successful
if [ $? -ne 0 ]; then
  echo "Error: .NET publish failed!"
  exit 1
fi

# Change file permissions
chmod -R a+rwX ./out/linux-x64/ # set read and write permissions to all files
chmod +x ./out/linux-x64/CrossPlatformDownloadManager.DesktopApp # set executable permissions to main executable

# Create .tar.gz from the published files
tar -czf Cross-platform.Download.Manager.linux-amd64.tar.gz -C ./out/linux-x64 .

# Check if tar.gz creation was successful
if [ $? -ne 0 ]; then
  echo "Error: Failed to create tar.gz file!"
  exit 1
fi

# Ensure Deploy/bin directory exists
mkdir -p ../bin

# Move the tar.gz file to Deploy/bin
mv Cross-platform.Download.Manager.linux-amd64.tar.gz ../bin/

# Check if move was successful
if [ $? -ne 0 ]; then
  echo "Error: Failed to move tar.gz to Deploy/bin!"
  exit 1
fi

# Clean-up
rm -rf ./out/
rm -rf ./Cross-platform.Download.Manager.linux-amd64.tar.gz

echo "Success: MyApp-linux.tar.gz created and moved to ../Deploy/bin/"