# Optical Link contract

An Optical Link turns normalized Core messages and tool definitions into a request accepted by one model provider, then normalizes the provider response.

Each configured connection has a host identity, provider type, default model preference, protected credential, and lifecycle metadata. A host may support multiple connections and define one default without making connection selection part of Core. A conversation may override the default model without changing the connection.

An implementation must:

- keep provider-specific types outside Core;
- resolve credentials only on the server side;
- report the model actually used;
- map tool calls without executing them remotely;
- enforce transport and response bounds;
- translate provider failures without leaking raw bodies or credentials.

Provider discovery, billing, quotas, and account configuration remain provider concerns. Supporting one Optical Link does not require a generic provider marketplace.

Listing models is optional. An adapter that implements discovery must preserve provider identifiers, expose relevant capabilities such as tool calling, and tolerate catalog or availability changes. Core must still accept an explicit bounded model identifier so discovery is never required for a connection to work.
