#!/bin/bash
BASE_DIR=$PWD
APP_NAME="dist_osx/Atomex.app"
PUBLISH_OUTPUT_DIRECTORY="bin/Release/net5.0/osx-x64/publish/."
INFO_PLIST="Info.plist"
ICON_FILE="logo.icns"

dotnet publish -r osx-x64 --configuration Release

if [ -d "$BASE_DIR/$APP_NAME" ]
then
    rm -rf "$BASE_DIR/$APP_NAME"
fi

mkdir -p "$BASE_DIR/$APP_NAME"

mkdir "$BASE_DIR/$APP_NAME/Contents"
mkdir "$BASE_DIR/$APP_NAME/Contents/MacOS"
mkdir "$BASE_DIR/$APP_NAME/Contents/Resources"

cp "$BASE_DIR/$INFO_PLIST" "$BASE_DIR/$APP_NAME/Contents/Info.plist"
cp "$BASE_DIR/$ICON_FILE" "$BASE_DIR/$APP_NAME/Contents/Resources/logo.icns"
cp -a "$BASE_DIR/$PUBLISH_OUTPUT_DIRECTORY" "$BASE_DIR/$APP_NAME/Contents/MacOS"