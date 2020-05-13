# Supported actions

:warning: It is possible to create loops in the system when forwarding mails to other systems that in turn forward the email as well (or generate more emails). Be very careful when setting actions that can cause possible loops. :warning:

A classic example is the inbox rule `forward all emails with subject <X>` in your private inbox and then having a rule `forward every email with subject <X> to <private inbox>` in this system.

Processing (Sendgrid + Azure function) has a bit of delay (at most 10 seconds per email) which means you probably won't bring down the mail server(s), but nevertheless end up with a bunch of mails in your inbox and a possible temporary suspension of your Sendgrid account. ;)

___

Actions are defined by the `type`, `id` and custom `properties`.

The configuration supports an arbitrary amount of actions with various filters and the system internally takes care of delivering the emails to each.

Each action must have a unique id across the entire file and the id should never change for an action as the id is to track retries in case of failures (see [Fault tolerance](Fault%20tolerance.md)).

## Email

For every email received at the domain it will forward it to another inbox without the need for a dedicated mail package at the domain.

It even allows you to conveniently respond to emails from the private inbox.

**Note:** When replying to such a mail from your private inbox the reply will be sent as a mail from your private inbox (and not the domain). If you want to send emails via your domain, check out my [email-relay](https://github.com/MarcStan/email-relay) which supports both sending and receiving emails from domain addresses (and can be hooked up behind this fanout system using the [Forward](#Forward) action instead).

Assuming your private inbox is `me@example.com` and you have setup this webhook on `<domain>` then this config will forward all emails sent to `<anything>@<domain>` to `me@example.com`:

``` json
{
    "type": "Email",
    "properties": {
        "sendgrid": {
            "secretName": "SendgridKey"
        },
        "domain": "<domain>",
        "targetEmail": "me@example.com"
    }
}
```

For every email sent to your domain this will send a new email to `me@example.com` (using sendgrid and your domain).

If e.g. `user@foo.com` sends a mail to `you@<domain>` this system will pick it up thanks to Inbound Parse and a will send a new email from `you@<domain>` to `me@example.com`. The sender thus lets you know which domain email was originally targeted.

(Unfortunately [sendgrid doesn't support](https://github.com/sendgrid/sendgrid-csharp/issues/890) the [Sender vs. From distinction](https://stackoverflow.com/a/4728446) which would allow to display the original sender inline via the `you@<domain> on behalf of <original sender>`).

Instead the `Reply-To` field will be set to the original author email. **When you hit reply it will magically appear in the `to` field (replacing `you@<domain>`) and you can easily respond to the email from your private inbox.**

A small change is made to the message when CC addresses are used as well - since they can't be easily displayed in the CC field (without also sending them a copy of the email) they are instead prefixed in the body of the message:

```
CC: <possible@other.recipients>; <are@listed.here>
__________
%original content%
```
You will have to manually cut them and paste them in the CC line and remove the text from the email to properly respond to the email.

Lastly you can combine this action with [filters](Supported%20filters.md) to setup multiple separate inboxes (e.g. forward `work@<domain>` to `<work inbox>` and forward `me@<domain>` to `<private inbox>`).

## Forward

Forwards the received email as is to another webhook. The other webhook can then parse the exact same sendgrid message again.

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

Supported targets that I have written include [email-bug-tracker](https://github.com/MarcStan/email-bug-tracker) and [email-relay](https://github.com/MarcStan/email-relay).

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

When a specific email is received a notification is sent to a webhook. The webhook format is much simpler than the one from [Forward](#Forward) saving the recipient the hassle of parsing the sendgrid format.

Content of the original email can be inserted with `%subject%`, `%sender%` and `%body%` placeholders allowing for some basic transformations.

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

The webhook url must be stored in a keyvault secret and the target webhook must receive POST requests and can expect the following format:

``` json
{
    "sender": "name/email",
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
___

See [Supported filters](Supported%20filters.md) for a list of all possible filters.
