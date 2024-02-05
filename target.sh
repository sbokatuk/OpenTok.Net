if [ -z "$1" ]
then
echo "No target Argument for Opentok"
else
sed -E -i "" "s/<TargetFramework>net([0-9]{1,}\.)+[0-9]{1,}-ios/<TargetFramework>net$1-ios/" Bindings/OpenTok.Net.iOS/OpenTok.Net.iOS.csproj
sed -E -i "" "s/<TargetFramework>net([0-9]{1,}\.)+[0-9]{1,}-android/<TargetFramework>net$1-android/" Bindings/OpenTok.Net.Android/OpenTok.Net.Android.csproj

if [[ "$1" == "7.0" ]]
then
rm NugetPackages/OpenTok.Net.nuspec
cp -R NugetPackages/OpenTok.Net.nuspec.7.0 NugetPackages/OpenTok.Net.nuspec
fi

if [[ "$1" == "8.0" ]]
then
rm NugetPackages/OpenTok.Net.nuspec
cp -R NugetPackages/OpenTok.Net.nuspec.8.0 NugetPackages/OpenTok.Net.nuspec
fi

echo "Target for Opentok set to $1"
fi