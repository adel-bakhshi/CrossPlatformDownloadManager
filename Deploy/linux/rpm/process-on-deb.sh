#!/bin/bash

# Clean-up
echo "Cleaning up..."
rm -rf ./Deploy/linux/rpm/out/
rm -rf ./Deploy/linux/rpm/tmp/
rm -rf ~/rpmbuild/
rm -rf ./Deploy/bin/*.rpm

# .NET publish
# self-contained is recommended, so final users won't need to install .NET
echo "Publishing app..."
dotnet publish "./CrossPlatformDownloadManager.DesktopApp/CrossPlatformDownloadManager.DesktopApp.csproj" \
  --verbosity quiet \
  --nologo \
  --configuration Release \
  --self-contained true \
  --runtime linux-x64 \
  --output "./Deploy/linux/rpm/out/cross-platform-download-manager-0.5.0"

# Create directories
echo "Creating reqiured directories..."
mkdir -p ~/rpmbuild/{BUILD,RPMS,SOURCES,SPECS,SRPMS}
mkdir -p ./Deploy/linux/rpm/tmp/assets/

# Copy spec file
echo "Copying spec file..."
cp ./Deploy/linux/rpm/app.spec ~/rpmbuild/SPECS/cross-platform-download-manager.spec

# Create tar.gz file
echo "Creating tarball files..."
cp -f -a ./Deploy/linux/basics/icons/. ./Deploy/linux/rpm/tmp/assets/ # Copy icons
cp ./Deploy/linux/basics/app.desktop ./Deploy/linux/rpm/tmp/assets/cross-platform-download-manager.desktop # Copy desktop file
cp ./Deploy/linux/basics/starter-script.sh ./Deploy/linux/rpm/tmp/assets/cross-platform-download-manager # Copy starter script
mv ./Deploy/linux/rpm/tmp/assets ./Deploy/linux/rpm/out/cross-platform-download-manager-0.5.0/
tar -czvf ./Deploy/linux/rpm/cross-platform-download-manager-0.5.0.tar.gz -C ./Deploy/linux/rpm/out/ .
mv ./Deploy/linux/rpm/cross-platform-download-manager-0.5.0.tar.gz ~/rpmbuild/SOURCES/

# Build the RPM package
echo "Building rpm package..."
rpmbuild -ba ~/rpmbuild/SPECS/cross-platform-download-manager.spec

# Create output directory
echo "Creating output directory..."
mkdir -p ./Deploy/bin/

# Move the RPM package to the output directory
echo "Moving RPM package to the output directory..."
mv ~/rpmbuild/RPMS/x86_64/*.rpm ./Deploy/bin/Cross-platform.Download.Manager.linux-x86_64.rpm

