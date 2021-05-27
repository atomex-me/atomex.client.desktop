#!/bin/bash
BASE_DIR=$PWD
DIST_FOLDER="dist_osx"
APP_NAME="$DIST_FOLDER/Atomex.app"
ZIP_PATH="$DIST_FOLDER/Atomex.zip"
PUBLISH_OUTPUT_DIRECTORY="bin/Release/net5.0/osx-x64/publish/."
INFO_PLIST="Info.plist"
ICON_FILE="logo.icns"
LAUNCHD_FILE="com.atomex.osx.plist"

dotnet clean -c Release
dotnet restore
dotnet publish -r osx-x64 --configuration Release
dotnet tool install --global NetSparkleUpdater.Tools.AppCastGenerator --version 2.0.6

rm -rf "$BASE_DIR/$DIST_FOLDER"
mkdir "$BASE_DIR/$DIST_FOLDER"
mkdir -p "$BASE_DIR/$APP_NAME"

mkdir "$BASE_DIR/$APP_NAME/Contents"
mkdir "$BASE_DIR/$APP_NAME/Contents/MacOS"
mkdir "$BASE_DIR/$APP_NAME/Contents/Resources"

cp "$BASE_DIR/$INFO_PLIST" "$BASE_DIR/$APP_NAME/Contents/$INFO_PLIST"
cp "$BASE_DIR/$ICON_FILE" "$BASE_DIR/$APP_NAME/Contents/Resources/$ICON_FILE"
cp -a "$BASE_DIR/$PUBLISH_OUTPUT_DIRECTORY" "$BASE_DIR/$APP_NAME/Contents/MacOS"


echo "[INFO] Signing Code"
ENTITLEMENTS="AtomexEntitlements.entitlements"
SIGNING_IDENTITY="Developer ID Application: ATOMEX OU (BJT6S7XYJV)" # matches Keychain Access certificate name

find "$APP_NAME/Contents/MacOS/"|while read fname; do
    if [[ -f $fname ]]; then
        echo "[INFO] Signing $fname"
        codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$fname"
    fi
done
echo "[INFO] Signing app file"

codesign --force --timestamp --options=runtime --entitlements "$ENTITLEMENTS" --sign "$SIGNING_IDENTITY" "$APP_NAME"

echo "[INFO] Creating ZIP Archive"
rm -f "$BASE_DIR/$ZIP_PATH"
/usr/bin/ditto -c -k --sequesterRsrc --keepParent "$APP_NAME" "$ZIP_PATH"

dev_account="im@atomex.me"
password="$1"
identifier="com.atomex.osx"

# functions
request_status() { # $1: requestUUID
    requestUUID=${1?:"need a request UUID"}
    req_status=$(xcrun altool --notarization-info "$requestUUID" \
                              --username "$dev_account" \
                              --password "$password" 2>&1 \
                 | awk -F ': ' '/Status:/ { print $2; }' )
    echo "$req_status"
}

notarize_file() { # $1: path to file to notarize, $2: identifier
    filepath=${1:?"need a filepath"}
    identifier=${2:?"need an identifier"}

    # upload file
    echo "## uploading $filepath for notarization"
    requestUUID=$(xcrun altool --notarize-app \
                               --primary-bundle-id "$identifier" \
                               --username "$dev_account" \
                               --password "$password" \
                               --file "$filepath" 2>&1 \
                  | awk '/RequestUUID/ { print $NF; }')

    echo "Notarization RequestUUID: $requestUUID"

    if [[ $requestUUID == "" ]]; then 
        echo "could not upload for notarization"
        exit 1
    fi

    # wait for status to be not "in progress" any more
    request_status="in progress"
    while [[ "$request_status" == "in progress" ]]; do
        echo -n "waiting... "
        sleep 10
        request_status=$(request_status "$requestUUID")
        echo "$request_status"
    done

    # print status information
    xcrun altool --notarization-info "$requestUUID" \
                 --username "$dev_account" \
                 --password "$password"
    echo 

    if [[ $request_status != "success" ]]; then
        echo "## could not notarize $filepath"
        exit 1
    fi
}

# upload for notarization
notarize_file "$ZIP_PATH" "$identifier"

# staple result
echo "## Stapling $APP_NAME"
xcrun stapler staple "$APP_NAME"

echo "## creating and notarizing DMG"

create-dmg "$APP_NAME" "$DIST_FOLDER"
dmg_file_name=$(find "$DIST_FOLDER" -type f -name "*.dmg")
notarize_file "$dmg_file_name" "$identifier"

echo "## Stapling $dmg_file_name"
xcrun stapler staple "$dmg_file_name"

export PUB_DATE=$(date -u +"%a, %d %b %Y %T GMT")
export VER=$(xmllint --xpath "//Project/PropertyGroup/Version/text()" Atomex.Client.Desktop.csproj)
export DESCRIPTION="Atomex release ${VER} ${PUB_DATE}"
export SIGNATURE=$(netsparkle-generate-appcast --generate-signature ${ZIP_PATH} | awk '{print $2}')
export ZIP_SIZE=$(ls -la ${ZIP_PATH} | awk '{print $5}')

envsubst < appcast.xml > dist_osx/appcast.xml

echo "## ZIP SIGNATURE: ${SIGNATURE}"
echo "## ZIP SIZE: ${ZIP_SIZE}"

rm -rf "$APP_NAME"
echo '## Done!'
