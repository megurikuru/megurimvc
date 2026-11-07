systemctl stop kestrel-meguri-fans.service

dotnet publish --configuration Release
rm -rf /var/www/meguri-fans
cp -pr /home/meguri/repos/megurimvc/Meguri/bin/Release/net10.0/publish /var/www/meguri-fans
chown -R www-data:www-data /var/www/meguri-fans

systemctl start kestrel-meguri-fans.service
