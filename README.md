# Sonicwall-Interface

## About
This program is meant to automate configuration of a SonicWall firewall via the [SonicOS TI API](https://www.sonicwall.com/support/knowledge-base/how-do-i-setup-and-use-the-threat-api-feature-on-my-firewall/171120113244716/).
Currently this program is just meant to insert IP's from our Azure Sentinel TI pool.
The program triggers by listening to a toppic on a message provider. (Currently only Azure Service Bus)

## Instalation

W.I.P.

## Configuration
```
{
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
        "MinConfidence": 25
    },
    "ServiceBusConfig":
    {
        "ConnectionString": "CONNECTION_STRING"
    }
}
```

## Notes
Future development options:
- RabbitMQ
- Other TI Sources
- Non TI API (probably not)
