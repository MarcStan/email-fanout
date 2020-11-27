# v1.1.0

* added enabled flag to rules, filters & actions
# v1.0.0

* breaking change: webhook format

old format:

``` json
{
    "id": "notify-mail",
    "type": "Webhook",
    "properties": {
        "webhook": {
            "secretName": "MatrixWebhook"
        },
        // properties are first class
        "subject": "You've got mail!",
        "body": "%subject%",
        // enum selecting if attachments should be kept
        "attachments": "keep|drop"
    }
}
```

new format:

``` json
{
    "id": "notify-mail",
    "type": "Webhook",
    "properties": {
        "webhook": {
            "secretName": "MatrixWebhook"
        },
        // separate property, all its content can be customize
        // and will be sent to webhook as json payload
        "body": {
            "subject": "You've got mail!",
            "body": "%subject%",
            // "%attachments%" will be replaced by array of attachments
            "attachments": "%attachments%"
        }
    }
}
```

# v0 (preview)

* implemented webhook, forward, archive & email actions
* custom filters (sender, content, negate, oneOf, allOf, ..)
