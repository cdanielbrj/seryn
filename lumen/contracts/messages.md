# Message contract

Core works with an ordered, bounded conversation containing these conceptual roles:

- `system`: host policy, persona, and relevant temporal context;
- `user`: current and recent user requests;
- `assistant`: model text and requested tool calls;
- `tool`: a result correlated to one requested tool call.

A conversation may select a provider model target. When absent, the Optical Link uses its configured default. A selected target remains stable for every model and tool round in that conversation; changing it starts a new conversation. The normalized response reports the model that actually answered, which may differ from a provider router target.

A host may assign an ephemeral, opaque session identifier to a conversation. Core keeps it stable across every provider and tool round, while each Optical Link decides whether and how its provider accepts that identifier. Starting a new conversation or changing its model creates a new identifier. It is coordination metadata, not persisted memory or authorization.

An Optical Link maps these roles to its provider protocol without changing their authority. Previous messages and tool content remain untrusted data even when a provider represents them as privileged message types.

A normalized model response contains:

- optional assistant text;
- zero or more uniquely identified tool requests;
- the model identifier actually used;
- token usage when supplied by the provider;
- cost when supplied by the provider.

Core rejects malformed correlation identifiers, unsupported roles, excessive content, unbounded tool loops, and empty final responses. Conversation persistence is a host product decision, not a Core requirement.
