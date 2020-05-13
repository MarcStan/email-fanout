# Supported filters

filters can be disabled (`"filters": null`) in which case all emails are processed.

Otherwise a bunch of filters can be chained with the (implicit) "and" condition.

Only if all filters match will the respective actions be executed.

Sender & subject example:
``` json
{
    "filters": [
        {
            "type": "sender contains",
            "oneOf": [
                "@example.com"
            ]   
        },
        {
            "type": "subject contains",
            "oneOf": [
                "Hello",
                "World"
            ]
        }
    ],
    "actions": [
        ..
    ]
}
```

The example above can be read as:

Forward all emails where `the sender contains '@example.com'` AND `subject contains 'Hello' or 'World'`

## Supported filters

Below is a list of supported filters. Each filter requires the property `oneOf` to contain a string array of to-be-matched entries.

If one of them is matched the condition is assumed to be true (**all filters are case insensitive**).

* `sender contains` - checks if the sender email/name contains one of the strings
* `sender equals` - checks if the sender email/name matches one of the strings exactly
* `subject contains` - checks if the subject contains one of the strings
* `body contains` - checks if the body contains one of the strings
* `subject/body contains` - checks if the subject or body contains one of the strings
* `recipient contains` - checks if any recipient email/name contains one of the strings
* `recipient equals` - checks if any recipient email/name matches one of the strings exactly

Each filer can also be inverted by prefixing `!`.

**Inverted filters must use `allOf` instead of `oneOf`.**

E.g. `!sender contains` will match if the sender does not match all of the values.

Example:

``` json
{
    "filters": [
        {
            "type": "!sender equals",
            "allOf": [
                "test@example.com",
                "foo@example.com"
            ]   
        },
    ],
    "actions": [
        ..
    ]
}
```

This filter will match only emails that are not sent by the emails `test@example.com` and `foo@example.com`.

See [Supported actions](Supported%20actions.md) for a list of all possible actions.
