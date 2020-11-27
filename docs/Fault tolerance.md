# Fault tolerance

The system can handle deliver failures gracefully as each target is tracked independently - a failure in one target does not impact the other targets and each target is retried independently.

The overall system guarantees  delivery for each target (assuming the target accepts the webhook by responding with HTTP 200 within 72 hours).

In case of at least one action failing the function will retry it by relying on the [retry behaviour of sendgrid](https://sendgrid.com/docs/for-developers/parsing-email/inbound-email/).

Each action is tried and successful actions are marked as such (however the system does **not guarantee exactly-once delivery** *). In case one or more actions failed the function responds with 400 (Bad Request) to Sendgrid which will trigger the Sendgrid retry mechanism (multiple tries up to 72 hours).

On each retry the system will detect successful actions and skips their execution where possible.

Once all actions are successful the system marks them all as such and respons with 200 (Ok) to Sendgrid, letting it know that no (more) retries are necessary.

(*) When multiple targets are used and some targets fail you may see duplicate deliveries on other targets as the system can only guarantee **at least once delivery**. As retries may happen in parallel it is unlikely but possible that some targets receive the notification twice.
