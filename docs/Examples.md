# Examples

Here is an example that is close to what I'm using in production:

I set this function up as the main receiver of my domain and have it parse all emails.

The fanout system then:

* stores all emails in a storage account (both as a backup and in case the other systems have failures)
* forwards all emails to a private inbox allowing convenient responses from the private email
* forwards emails that where both sent by `me@<private>` and sent to `bugs@<domain>` to the [email-bug-tracker](https://github.com/MarcStan/email-bug-tracker)
* forwards emails that where both sent by `me@<private>` and sent to `matrix@<domain>` to a custom webhook (which in turn posts the messages to a matrix room)
  * Used incombination with an inbox forward rule i.e. I receive a specific email and forward it to the matrix inbox which then posts a message


``` json
{
    "rules": [
        {
            "comment": "Archive all emails",
            "filters": null,
            "actions": [
                {
                    "id": "archive-all",
                    "type": "Archive",
                    "properties": {
                        "containerName": "emails"
                    }
                }
            ]
        },
        {
            "comment": "Private inbox",
            "filters": null,
            "actions": [
                {
                    "id": "private-email",
                    "type": "Email",
                    "properties": {
                        "sendgrid": {
                            "secretName": "SendgridKey"
                        },
                        "fromEmail": "mail@<domain>",
                        "targetEmail": "me@example.com"
                    }
                }
            ]
        }
        {
            "comment": "Bug tracking",
            "filters": [
                {
                    "type": "sender equals",
                    "oneOf": [
                        "me@<private>"
                    ]
                },
                {
                    "type": "recipient equals",
                    "oneOf": [
                        "bugs@<domain>"
                    ]
                }
            ],
            "actions": [
                {
                    "id": "forward-bugs",
                    "type": "Forward",
                    "properties": {
                        "webhook": {
                            "secretName": "BugTrackerWebhook"
                        }
                    }
                }
            ]
        },
        {
            "comment": "Matrix notifications",
            "filters": [
                {
                    "type": "sender equals",
                    "oneOf": [
                        "me@<private>"
                    ]
                },
                {
                    "type": "recipient equals",
                    "oneOf": [
                        "matrix@<domain>"
                    ]
                }
            ],
            "actions": [
                {
                    "id": "notify-all",
                    "type": "Webhook",
                    "properties": {
                        "webhook": {
                            "secretName": "MatrixWebhook"
                        },
                        "subject": "You've got mail!",
                        "body": "%subject%"
                    }
                }
            ]
        }
    ]
}
```

As per the setup instructions the file should be stored in `config` container and be named `email-fanout.json`.
