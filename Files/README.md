# SAP × Microsoft Copilot Studio — MCP Gateway guides

A step-by-step series on connecting **Microsoft Copilot Studio** to SAP through the **MCP Gateway** on SAP Integration Suite — one of two integration architectures explicitly **endorsed by SAP** in the [SAP API Policy](https://help.sap.com/doc/sap-api-policy/latest/en-US/API_Policy_latest.pdf).

Each part builds on the previous one. The MCP server stays the same throughout (the public **Star Wars API**, same exposed tools) — what changes is **how identity flows to SAP**.

| # | Guide | What it adds | Identity at the gateway | Video |
|---|---|---|---|---|
| 1 | [MCP Gateway on SAP Integration Suite](./mcp-gateway-integration-suite-copilot-studio.md) | Build the MCP server; connect via **Azure API Management** using `client_credentials` | Shared technical account | [▶️ watch](https://youtu.be/1m12OVONavA) |
| 2 | [User authentication with Microsoft Entra ID](./mcp-gateway-entra-id-copilot-studio.md) | **OAuth 2.0 authorization code** with Entra ID; connect **directly** to the gateway | Real user — **Entra ID** | [▶️ watch](https://www.youtube.com/watch?v=jE-qlg2vZ6I) |
| 3 | [User authentication with SAP IAS (federated to Entra ID)](./mcp-gateway-ias-copilot-studio.md) | **SAP IAS** issues the token (Entra federated into IAS); the foundation for **on-prem principal propagation** | Real user — **SAP IAS** (SAP-native) | _coming soon_ |

## Where to start

- **New here?** Begin with **Part 1** to build the MCP server, then follow the series in order.
- **Already have the MCP server?** Jump to **Part 2** (Entra ID) or **Part 3** (SAP IAS).
- Parts 2 and 3 give the same result at the gateway — *user context* — but only the **IAS** token (Part 3) can travel further into SAP for on-prem principal propagation.

## Supporting artifacts

- [`swapi-openapi-301.yaml`](./swapi-openapi-301.yaml) — the OpenAPI 3.0.1 spec used to generate the MCP server (Parts 1–3).
- [`custom-connector-script.csx`](./custom-connector-script.csx) — C# fix for the `Content-Type: application/json; charset=utf-8` rejection in the auto-created custom connector (Parts 2–3).
- [`entra-id-auth.http`](./entra-id-auth.http) — REST Client snippets for the Entra ID OAuth flow (Part 2).
- [`verify-step1-discovery.http`](./verify-step1-discovery.http) · [`verify-step2-ias-token.http`](./verify-step2-ias-token.http) — REST Client snippets to verify the IAS flow (Part 3).

> The `.http` files ship with **placeholders only** — never commit real client secrets or authorization codes.
