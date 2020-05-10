# Sendgrid based fanout mechanism for incoming email

:warning: WIP & largely untested.

I have built multiple solutions that parse incoming emails to perform actions:

- [email-bug-tracker](https://github.com/MarcStan/email-bug-tracker) - Creates bug entries in Azure DevOps based on specially crafted emails
- [email-relay](https://github.com/MarcStan/email-relay) - Allows sending/receiving emails from your domains by (ab)using the SendGrid Inbound Parse feature
- matrix-email-bot - Forwards emails based on filter criteria to a [matrix room](https://matrix.org/try-now/) (to be opensourced in the future)

# Problem

Each service listens for incoming emails via the Sendgrid Inbound Parse feature.

But only one of these services can run on a domain at any point in time as sendgrid only allows one webhook per (sub)domain.

My current workaround is to use subdomains to set them all up (`<recipient>@example.com`, `<recipient>@bugs.example.com` & `<recipient>.matrix@example.com`).

If it where possible to set multiple webhooks on a single domain I would have to adjust all my functions since they all report errors when invalid emails are received (and with these 3 functions 2/3 would always report errors as the email wasn't intended for them).

# Solution

To receive emails on a single domain/address I built this Azure function based fanout system that can forward emails based on filters to the various other webhooks.

This function builds ontop of the Sendgrid [retry mechanism](https://sendgrid.com/docs/API_Reference/SMTP_API/errors_and_troubleshooting.html) (retry for 72 hours when error codes are received).

See [Fault tolerance](docs/Fault%20tolerance.md) for more details.

# Features

* fanout emails to multiple webhooks
* filtering to forward emails to specific targets only
* retry & failsafe
* multiple supported targets and formats (webhook, storage account)

___
:warning: When setting mx record of your root domain to sendgrid **all** emails will be relayed through sendgrid. If you have a regular mail client, **it will no longer receive emails!**

Additionally sendgrid free tier is limited to 100 mails per day (25000 mails per month if you signup via Azure) if this is not enough for you, consider a regular email service or the paid sendgrid plans.

You can also chose to only enable this system on a subdomain (e.g. foo.example.com) but then only emails of that subdomain will be received (e.g. bar@foo.example.com).

# Known issues

* attachment names with [non ascii characters are wrongly encoded if sent via sendgrid](https://github.com/sendgrid/sendgrid-go/issues/362) (the content is always correctly encoded, though)

# Setup

You must first setup a Sendgrid account and connect your domain (make sure that Sendgrid is able to send emails on behalf of your domain).

You can follow [their documentation](https://sendgrid.com/docs/ui/account-and-settings/how-to-set-up-domain-authentication/) to setup domain authentication.

Deployment is fully automated via Github actions. Just [setup credentials](https://github.com/marketplace/actions/azure-login#configure-azure-credentials), adjust the variables at the start of the yaml file (resourcegroup name) and run the action.

# Testing

Once the azure function is hooked up, all you have to do is send an email to your domain.

The email should then be stored in the storage account or relayed to the target webhooks (depending on your setup) within ~10 seconds.
