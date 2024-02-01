if [ -z "$1" ]
then
echo "No version Argument for Opentok"
else
sed -E -i "" "s/version = ([0-9]{1,}\.)+[0-9]{1,}/version = $1/" Bindings/iOS/MAUI.Opentok/MAUI.Opentok.sln
sed -E -i "" "s/<ReleaseVersion>([0-9]{1,}\.)+[0-9]{1,}/<ReleaseVersion>$1/" Bindings/iOS/MAUI.Opentok/MAUI.Opentok.csproj
echo "Version for Opentok set to $1"
fi