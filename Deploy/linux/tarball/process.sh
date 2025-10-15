#!/bin/bash

# Check if the correct number of arguments are provided
if [ $# -lt 2 ]; then
    echo "Error: Missing arguments!"
    echo "Usage: $0 <runtime> <version>"
    echo "Example: $0 linux-x64 v1.0.0"
    exit 1
fi

# Assign arguments
RUNTIME=$1
VERSION=$2

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed or not in PATH!"
    exit 1
fi

# Clean-up previous builds
rm -rf ./Deploy/linux/tarball/out/
# Remove each file in the current directory that starts with "Cross-platform.Download.Manager."
for file in ./Deploy/linux/tarball/Cross-platform.Download.Manager.*.$RUNTIME.tar.gz; do
    if [ -f "$file" ]; then
        rm "$file"
    fi
done

# Ensure all required directories exist
mkdir -p "./Deploy/linux/tarball/out/$RUNTIME"
mkdir -p "./Deploy/bin/"

# .NET publish
echo "Publishing for $RUNTIME..."
# self-contained is recommended, so final users won't need to install .NET
dotnet publish "./CrossPlatformDownloadManager.DesktopApp/CrossPlatformDownloadManager.DesktopApp.csproj" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:Version=$VERSION \
  -p:FileVersion=$VERSION \
  -p:AssemblyVersion=$VERSION \
  --runtime $RUNTIME \
  --output "./Deploy/linux/tarball/out/$RUNTIME"

# Check if publish was successful
if [ $? -ne 0 ]; then
  echo "Error: .NET publish failed!"
  exit 1
fi

# Change file permissions
echo "Setting file permissions..."
chmod -R a+rwX ./Deploy/linux/tarball/out/$RUNTIME/ # set read and write permissions to all files
chmod +x ./Deploy/linux/tarball/out/$RUNTIME/CrossPlatformDownloadManager.DesktopApp # set executable permissions to main executable

# Create .tar.gz from the published files
echo "Creating tarball..."
tar -czf ./Deploy/linux/tarball/Cross-platform.Download.Manager.$VERSION.$RUNTIME.tar.gz -C ./Deploy/linux/tarball/out/$RUNTIME .

# Check if tar.gz creation was successful
if [ $? -ne 0 ]; then
  echo "Error: Failed to create tar.gz file!"
  exit 1
fi

# Ensure Deploy/bin directory exists
mkdir -p ./Deploy/bin/

# Move the tar.gz file to Deploy/bin
mv ./Deploy/linux/tarball/Cross-platform.Download.Manager.$VERSION.$RUNTIME.tar.gz ./Deploy/bin/

# Check if move was successful
if [ $? -ne 0 ]; then
  echo "Error: Failed to move tar.gz to Deploy/bin!"
  exit 1
fi

# Success message
echo "Success: Cross-platform.Download.Manager.$VERSION.$RUNTIME.tar.gz created and moved to ./Deploy/bin/"