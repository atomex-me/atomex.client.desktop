#!/bin/bash
MSI_PATH=$(find dist/ -type f -name "*.msi")

export PUB_DATE=$(date -u +"%a, %d %b %Y %T GMT")
export DESCRIPTION="Atomex release ${VER} ${PUB_DATE}"
export MSI_SIGNATURE=$(netsparkle-generate-appcast --generate-signature ${MSI_PATH} | awk '{print $2}')
export MSI_SIZE=$(ls -la ${MSI_PATH} | awk '{print $5}')

envsubst < appcast_win.xml > dist/appcast_win.xml

echo "## MSI SIGNATURE: ${MSI_SIGNATURE}"
echo "## MSI SIZE: ${MSI_SIZE}"

echo '## Done MSI signing!'
