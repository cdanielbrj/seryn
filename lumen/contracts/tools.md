# Tool contract

A tool definition contains a stable name, a user-independent description, and a bounded argument schema. Tool names are host capabilities, not provider-specific prompt syntax.

A tool execution receives a correlated request and returns:

- the tool name;
- normalized arguments;
- a bounded structured result;
- optional host-owned source references.

The host tool catalog controls the allowlist. Core coordinates calls but does not infer authorization from model text. Unknown tools and invalid arguments return safe structured failures or terminate the round according to host policy.

Tools must invoke host application services. They must not bypass validation, recurrence rules, authorization, or transactional boundaries by querying or mutating persistence directly.

Results must state truncation, filters, periods, units, or other limits needed to prevent the model from presenting incomplete data as complete. Monetary and other exact domain calculations remain host responsibilities.
