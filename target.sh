if [ -z "$1" ]
then
echo "No target Argument for Opentok"
else
sed -E -i "" "s/<TargetFramework>net([0-9]{1,}\.)+[0-9]{1,}-ios/<TargetFramework>net$1-ios/" Bindings/OpenTok.Net.iOS/OpenTok.Net.iOS.csproj
sed -E -i "" "s/<TargetFramework>net([0-9]{1,}\.)+[0-9]{1,}-android/<TargetFramework>net$1-android/" Bindings/OpenTok.Net.Android/OpenTok.Net.Android.csproj
echo "Target for Opentok set to $1"
fi