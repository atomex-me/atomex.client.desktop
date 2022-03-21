#!/bin/bash
dotnet restore
dotnet publish Atomex.Client.Desktop.csproj -c Release -r linux-x64

sudo apt update
sudo apt install libxml2-utils
dotnet tool install --global NetSparkleUpdater.Tools.AppCastGenerator --version 2.0.6

VER=$(xmllint --xpath "/*[local-name() = 'Project']/*[local-name() = 'PropertyGroup']/*[local-name() = 'Version']/text()" Atomex.Client.Desktop.csproj)

BASE_DIR="dist/Atomex_${VER}_amd64"
mkdir -p $BASE_DIR/opt/Atomex.Client.Desktop
cp -r bin/Release/net5.0/linux-x64/publish/. $BASE_DIR/opt/Atomex.Client.Desktop/

mkdir -p $BASE_DIR/usr/share/icons/hicolor/64x64/apps/
mkdir -p $BASE_DIR/usr/share/icons/hicolor/128x128/apps/
mkdir -p $BASE_DIR/usr/share/icons/hicolor/256x256/apps/
cp Resources/Images/atomex_logo_64x64.png $BASE_DIR/usr/share/icons/hicolor/64x64/apps/avalonia-atomex.png
cp Resources/Images/atomex_logo_128x128.png $BASE_DIR/usr/share/icons/hicolor/128x128/apps/avalonia-atomex.png
cp Resources/Images/atomex_logo_256x256.png $BASE_DIR/usr/share/icons/hicolor/256x256/apps/avalonia-atomex.png

mkdir -p $BASE_DIR/usr/share/applications/
cp avalonia-atomex.desktop $BASE_DIR/usr/share/applications/

mkdir -p $BASE_DIR/DEBIAN
export VERSION=$VER
envsubst < control > $BASE_DIR/DEBIAN/control

cp postinst $BASE_DIR/DEBIAN/postinst

dpkg-deb --build $BASE_DIR

export PUB_DATE=$(date -u +"%a, %d %b %Y %T GMT")
export DESCRIPTION="Atomex release ${VER} ${PUB_DATE}"
export DEB_SIGNATURE=$(netsparkle-generate-appcast --generate-signature $BASE_DIR.deb | awk '{print $2}')
export DEB_SIZE=$(ls -la $BASE_DIR.deb | awk '{print $5}')

envsubst < appcast_linux.xml > dist/appcast_linux.xml

rm -rf $BASE_DIR

echo "## DEB SIGNATURE: ${DEB_SIGNATURE}"
echo "## DEB SIZE: ${DEB_SIZE}"
echo '## Done DEB signing!'
