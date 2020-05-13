# Examples

Here is an example that is close to what I'm using in production:

I set this function up as the Sendgrid Inbound Parse webhook of my domain and have it process all received emails.

I have then configured it to fanout my emails to these targets:

* storage - stores all emails (both as a backup and in case the other systems have failures)
* email - forwards all emails to a private inbox allowing convenient & direct response from the private email
* bug tracking - forwards emails that where both sent by `me@example.com` and sent to `bugs@<domain>` to the [email-bug-tracker](https://github.com/MarcStan/email-bug-tracker)
* matrix notifications - forwards emails that where both sent by `me@example.com` and sent to `matrix@<domain>` to a custom webhook (which in turn posts the messages to a matrix room)
  * Used in combination with an inbox forward rule i.e. I receive a specific email and forward it to the matrix inbox which then posts a message to a room

The respective configuration looks like this:

``` json
{
    "rules": [
        {
            "comment": "Archive all emails in storage",
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
            "comment": "Forward all emails to my private inbox",
            "filters": null,
            "actions": [
                {
                    "id": "private-email",
                    "type": "Email",
                    "properties": {
                        "sendgrid": {
                            "secretName": "SendgridKey"
                        },
                        "domain": "<domain>",
                        "targetEmail": "me@example.com"
                    }
                }
            ]
        }
        {
            "comment": "Forward my bug reports to the tracker",
            "filters": [
                {
                    "type": "sender equals",
                    "oneOf": [
                        "me@example.com"
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
            "comment": "Post notifications about specific received emails in a matrix room",
            "filters": [
                {
                    "type": "sender equals",
                    "oneOf": [
                        "me@example.com"
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
                    "id": "notify-mail",
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
