# Sonicwall-Interface

## About
This program is meant to automate configuration of a SonicWall firewall via the [SonicOS TI API](https://www.sonicwall.com/support/knowledge-base/how-do-i-setup-and-use-the-threat-api-feature-on-my-firewall/171120113244716/).
Currently this program is just meant to insert IP's from our Azure Sentinel TI pool.
The program triggers by listening to a toppic on a message provider. (Currently only Azure Service Bus)

## Instalation

Currently the app is bundled with the runtime environment and does not need any installation. However if you want to install it as a service/daemon follow the instructions in the next sections.

## Usage

### Windows
- Grab the latest windows release and place it in a folder together with the `appsettings.json`.
- Configure the appsettings to your liking.
- Run the `SonicWallInterface.exe`

#### Setup service
- Use the `sc.exe` (Windows Service Control Manager) to create a windows service: `sc.exe create "Sonic Wall Interface" binpath="C:\Path\To\SonicWallInterface.exe --contentRoot C:\Path\To\appsettings.json"`
- More info can be found [here](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service#create-the-windows-service).

### Linux
- Grab the latest windows release and place it in a folder together with the `appsettings.json`.
- Configure the appsettings to your liking.
- Make the `SonicWallInterface` file executable with `chmod +x SonicWallInterface`.
- Run the executable.

#### Setup daemon
- After downloading the file create a user to run the service
- Finaly create a the `sonic_int.service` file inside `/etc/systemd/system/`
- Use the following template as input for your `sonic_int.service` file.
```
[Unit]
Description=ingest_threat_intel
After=network.target

[Service]
User=USER_ID
Group=GROUP_ID
ExecStart=/Location/Of/Executable/SonicWallInterface
Type=simple

[Install]
WantedBy=default.target
```

## Configuration
```
{
    "Logging": 
    {
        "LogLevel": 
        {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Hosting.Lifetime": "Information"
        }
    },
    "SonicWallConfig":
    {
        "FireWallEndpoint": "https://127.0.0.1",
        "Username": "admin",
        "Password": "password",
        "ValidateSSL": true
    },
    "ThreatIntelApiConfig":
    {
        "ClientId": "CLIENT_ID",
        "TenantId": "TENANT_ID",
        "ClientSecret": "CLIENT_SECRET",
        "WorkspaceId": "WORKSPACE_ID", //Only needed if you use the non graph API
        "MinConfidence": 25,
        "ExclusionListAlias": "WATCH_LIST_ALIAS", //Optional
        "IPv4CollumName": "IPv4_COLLUM_NAME" //Optional
    },
    "AppConfig": {
        "SiteName": "TestSite"
    },
    "ServiceBusConfig":
    {
        "ConnectionString": "CONNECTION_STRING"
    }
}
```

## Notes
A few things you should know. 
- At the moment collecting the TI from the Graph API does not work.
- It is not reccomended to set the MinConfidence at 0. (A lot of IPs will be given.)

## Future
- RabbitMQ
- Other TI Sources
- Docker image
- Install script
- Fix Graph API
- Support Exclusions
- Support multiple SonicWalls