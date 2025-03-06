#!/bin/bash

# Clean-up
echo "Cleaning up..."
rm -rf ./Deploy/rpm/out/
rm -rf ./Deploy/rpm/tmp/
rm -rf ~/rpmbuild/

# .NET publish
# self-contained is recommended, so final users won't need to install .NET
echo "Publishing app..."
dotnet publish "./CrossPlatformDownloadManager.DesktopApp/CrossPlatformDownloadManager.DesktopApp.csproj" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime linux-x64 \
  --output "./Deploy/rpm/out/cross-platform-download-manager-0.3.0"

# Install dependencies
echo "Installing dependencies"
sudo apt install rpm-build rpmdevtools

# Create directories
echo "Creating reqiured directories..."
rpmdev-setuptree
mkdir -p ./Deploy/rpm/tmp/assets/

# Copy spec file
echo "Copying spec file..."
cp ./Deploy/rpm/app.spec ~/rpmbuild/SPECS/cross-platform-download-manager.spec

# Create tar.gz file
echo "Creating tarball files..."
cp -f -a ./Deploy/rpm/icons/. ./Deploy/rpm/tmp/assets/ # Copy icons
cp ./Deploy/rpm/app.desktop ./Deploy/rpm/tmp/assets/cross-platform-download-manager.desktop # Copy desktop file
cp ./Deploy/rpm/starter-script.sh ./Deploy/rpm/tmp/assets/cross-platform-download-manager # Copy starter script
mv ./Deploy/rpm/tmp/assets ./Deploy/rpm/out/cross-platform-download-manager-0.3.0/
tar -czvf ./Deploy/rpm/cross-platform-download-manager-0.3.0.tar.gz -C ./Deploy/rpm/out/ .
mv ./Deploy/rpm/cross-platform-download-manager-0.3.0.tar.gz ~/rpmbuild/SOURCES/

# Build the RPM package
echo "Building rpm package..."
rpmbuild -ba ~/rpmbuild/SPECS/cross-platform-download-manager.spec

