# Fault tolerance

The system can handle deliver failures gracefully as each target is tracked independently - a failure in one target does not impact the other targets and each target is retried independently.

In case of at least one action failing the function will retry it by relying on the [retry behaviour of sendgrid](https://sendgrid.com/docs/for-developers/parsing-email/inbound-email/).

Each action is tried and successful actions are marked as such. In case one or more actions failed the function responds with 400 (Bad Request) to Sendgrid which will trigger the Sendgrid retry mechanism (multiple tries up to 72 hours).

On each retry the system will detect successful actions and does not execute them while executing actions that have previously failed.

Once all actions are successful the system marks them all as such and respons with 200 (Ok) to Sendgrid, letting it know that no (more) retries are necessary.
