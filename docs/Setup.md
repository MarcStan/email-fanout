# Setup

Create a new resourcegroup in Azure and configure the deployment credentials as per [Azure login](https://github.com/Azure/login).

Use the [Github Action](../.github/workflows/azure-functionapp.yml) to deploy the function to azure (you need to adjust the `ResourceGroup` variable in it to a unique name; all resources will be named based of that).

The configuration is a mix of keyvault and storage account (by default the `config` container in the storage account is used).

The general config should be placed in the storage container while the secrets should be stored in the keyvault.

The config file looks like this:

``` json
{
    "rules": [
        {
            "comment": "This target forwards all emails to another webhook (description for yourself what this target does)",
            "filters": null,
            "actions": [
                {
                    "type": "forward",
                    "properties": {
                        "webhook": {
                            "secretName": "Webhook1"
                        }
                    }
                }
            ]
        }
    ]
}
```

Each rule consists of the required property `actions`, an optional `filters` and you can use `comment` to include a an optional description for yourself.

In the example above a single action is defined. It is of type `forward` and will be executed for each received email because `filters` is null.

Furthermore `secretName` defines the name of the variable in the keyvault that contains the webhook.

This indirection is used because the webhook usually contains a secret (authcode, ..) which shouldn't be stored in the storage account.

See [Supported filters](Supported%20filters.md) for a list of all possible filters.

See [Supported actions](Supported%20actions.md) for a list of all possible actions.