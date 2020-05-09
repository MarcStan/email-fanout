# Supported actions

Actions are defined by the `type` and custom `properties`.

The configuration supports an arbitrary amount of actions and the system internally takes care of delivering the emails to each.

### forward

Forwards the email as is to another webhook.

The webhook url must be stored in a keyvault secret and the webhook must receive POST requests with the same body as this function (= Sendgrid Inbound Parse format).

``` json
{
    "type": "forward",
    "properties": {
        "webhook": {
            "secretName": "Webhook1"
        }
    }
}
```

### archive

Stores each email in blob storage.

Will store emails in format: `yyyy-MM`/`dd`/`HH-mm-ss_SENDER - SUBJECT.json`

Attachments are stored in subdirectories named identical to the json file (`yyyy-MM`/`dd`/`HH-mm-ss_SENDER - SUBJECT.json`/`attachment1.txt`)

``` json
{
    "type": "archive",
    "properties": {
        "containerName": "emails"
    }
}
```

### notify

When a specific email is received a notification is forwarded.

Content of the original email can be inserted with `%subject%`, `%sender%` and `%body%` placeholders.

Attachments can either be kept or dropped.

``` json
{
    "type": "notify",
    "properties": {
        "subject": "Notification",
        "body": "Email from %sender% regarding '%subject%'",
        "attachments": "keep|drop"
    }
}
```