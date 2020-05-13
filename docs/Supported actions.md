# Supported actions

:warning: It is possible to create loops in the system when forwarding mails to other systems that in turn forward the email as well (or generate more emails). Be very careful when setting actions that can cause possible loops. :warning:

A classic example is the inbox rule `forward all emails with subject <X>` in your private inbox and then having a rule `forward every email with subject <X> to <private inbox>`.

The system has a bit of delay (at most 10 seconds per email) which means you probably won't bring down the mail server(s), but nevertheless end up with a bunch of mails in your inbox and a possible temporary suspension on your Sendgrid account. ;)

___

Actions are defined by the `type`, `id` and custom `properties`.

The configuration supports an arbitrary amount of actions and the system internally takes care of delivering the emails to each.

Each action must have a unique id across the file and the id should not change for any action as it is used in case of retries (see [Fault tolerance](Fault%20tolerance.md)).

## Forward

Forwards the email as is to another webhook. The other webhook can then parse the exact same sendgrid message anew.

The webhook url must be stored in a keyvault secret and the webhook must receive POST requests with the same body as this function (= Sendgrid Inbound Parse format).

``` json
{
    "type": "Forward",
    "id": "unique-id",
    "properties": {
        "webhook": {
            "secretName": "Webhook1"
        }
    }
}
```

## Archive

Stores each email in blob storage (email along with all its headers in json format, attachments as separate files).

Will store emails in format: `yyyy-MM`/`dd`/`HH-mm-ss_SENDER - SUBJECT.json`

Attachments are stored in subdirectories named identical to the json file (`yyyy-MM`/`dd`/`HH-mm-ss_SENDER - SUBJECT.json`/`attachment-name.txt`)

``` json
{
    "type": "Archive",
    "properties": {
        "containerName": "emails"
    }
}
```

## Webhook

When a specific email is received a notification is sent to a webhook.

Content of the original email can be inserted with `%subject%`, `%sender%`and `%body%` placeholders.

Attachments can either be kept or dropped (they are dropped by default).

``` json
{
    "type": "Webhook",
    "properties": {
        "webhook": {
            "secretName": "Webhook2"
        },
        "subject": "Notification",
        "body": "Email from %sender% regarding '%subject%'",
        "sender": "sender was %sender%",
        "attachments": "keep|drop"
    }
}
```

The webhook url must be stored in a keyvault secret and the webhook must receive POST requests with the following format (attachment format is identical to sendgrid):

``` json
{
    "sender": "bloah",
    "subject": "foo",
    "body": "bar",
    "attachments": [
        {
            "id": "",
            "fileName": "",
            "base64Data": "",
            "content-id": "",
            "type": ""
        }
    ]
}
```

## Email

Send an actual email to a target using sendgrid.

``` json
{
    "type": "Email",
    "properties": {
        "sendgrid": {
            "secretName": "SendgridKey"
        },
        "targetEmail": "me@example.com"
    }
}
```

This will use sendgrid to relay the email to the provided address.

It will spoof the sender to allow you to easily respond to the email (but beware that the email will likely be marked as spam).

If you do not want the email to be spoofed, consider adding [email-relay](https://github.com/MarcStan/email-relay) as a webhook and have it relay the emails.
