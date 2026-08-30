systemctl stop kestrel-meguri.service

dotnet publish --configuration Release
rm -rf /var/www/meguri
cp -pr bin/Release/net10.0/publish /var/www/meguri
chown www-data:www-data /var/www/meguri

systemctl start kestrel-meguri.service
