# MCP Gateway on SAP Integration Suite

Connect **Microsoft Copilot Studio** to SAP in a fully endorsed and compliant way using the new **MCP Gateway** in SAP Integration Suite.

📺 **Watch the video:** <https://youtu.be/1m12OVONavA>

This guide accompanies the video *"MCP Gateway on BTP Integration Suite"* and walks you through building the setup from scratch: an MCP server on SAP Integration Suite exposing an external OData service (the public **Star Wars API**), consumed from Copilot Studio through **Azure API Management** as an intermediate proxy for authentication and throttling.

> Because we connect to an **external** OData service in this scenario, the SAP API Policy does not even apply. The MCP Gateway is one of two architectures explicitly **endorsed by SAP** in the [SAP API Policy](https://help.sap.com/doc/sap-api-policy/latest/en-US/API_Policy_latest.pdf).

---

## Architecture

```
Copilot Studio  ──▶  Azure API Management  ──▶  MCP Gateway (SAP Integration Suite / Integration Cell)  ──▶  SWAPI (Star Wars API)
   (agent)            (auth + API key + throttling)      (client_credentials, scope API.invoke)                (external API)
```

- **SAP Integration Suite – MCP Gateway**: hosts the MCP server, generated from the OpenAPI spec of the target API.
- **Azure API Management (APIM)**: performs the OAuth `client_credentials` token flow toward SAP, caches the token, forwards the bearer token to the MCP Gateway, and protects the endpoint with an API key. Chosen for flexibility with authentication and additional throttling.
- **Copilot Studio**: adds the MCP server (via the APIM endpoint) as a tool for an agent.

---

## Prerequisites

- An SAP BTP account (the MCP Gateway is available on the **BTP trial** environment, so anyone can try it).
- SAP **Integration Suite** provisioned with the required capabilities, roles assigned to your user, and the **Integration Cell** enabled. See [Activate the Integration Cell](https://help.sap.com/docs/integration-suite/isuite-integrations-and-apis/activate-integration-cell?version=CLOUD).
- An **Azure API Management** instance.
- **Microsoft Copilot Studio** access.
- VS Code with the **REST Client** extension (for testing).

---

## Step-by-step

### 1. Create the integration package and MCP server
1. In Integration Suite, go to **Design → Integrations and APIs** and create an **integration package** (e.g. `MCP test package`) with a technical name and short description. This package holds all artifacts.
2. Add an **MCP Server** artifact to the package.

### 2. Connect the external API
1. From the MCP server, add a connection to an external API. (You could also connect to APIs already proxied by SAP API Management.)
2. Provide the URL and a name (`SWAPI`) and the target for the public **Star Wars API** ([swapi.info](https://swapi.info/)).
3. Provide the **MCP path** — this is the path your client will call later.

### 3. Upload the OpenAPI specification
The MCP server does not know about the available tools/resources until you describe the API.
1. Click **Edit → Edit API specification**.
2. Upload the OpenAPI spec: [`swapi-openapi-301.yaml`](./swapi-openapi-301.yaml) (included in this folder).
3. Go to **Tools** and select the operations to expose: `films`, `people`, `planets`, `species`, `vehicles`, `starships`. Leave out the `*-schema` operations — they don't need to be exposed via MCP.

### 4. Review policies and deploy
- Under **Policies** you'll find the out-of-the-box configuration:
  - **Authentication** step (client certificate or OAuth).
  - **Authorization** step that checks for a specific **scope** (`API.invoke`).
- For this walkthrough, keep the defaults. **Save** and **deploy** the MCP server.

### 5. Create a service key with the right scope
1. In the **BTP Cockpit**, use the **Integration Suite booster** to create a **service key** — this gives you a **client ID** and **client secret**.
2. Ensure the scope queried by the policy is in place: open **Update**, and in the **Parameters** tab set the role/scope to **`API.invoke`**.

> The access token minted from this client ID/secret will then carry the `API.invoke` authority required by the MCP server's authorization step. You can verify the scopes/authorities with any JWT decoder.

### 6. Test with VS Code (REST Client)
Use the [REST snippets below](#vs-code--http-rest-calls) to:
1. Fetch an access token from the SAP token endpoint (`client_credentials`).
2. Call the MCP endpoint `initialize` — expect a success reply from the MCP Gateway.
3. Call `tools/list` — expect the full list of Star Wars tools back.

### 7. Proxy through Azure API Management
Rather than connecting Copilot Studio directly to BTP (which needs the OAuth flow), put **Azure APIM** in front. The APIM policy performs the **same authentication flow**: it fetches the SAP access token, caches it, and forwards it to the MCP Gateway. See the [Azure APIM policy below](#azure-api-management-policy).
- APIM exposes a **new MCP endpoint** for Copilot Studio.
- Protect the API with an **API key** (`X-API-key` header).

### 8. Consume from Copilot Studio
1. In Copilot Studio, open (or create) an agent and **add a tool → MCP server**.
2. Give it a **name** (`Star Wars API`) and a good **description** — the description tells Copilot Studio when to call this MCP server (e.g. for Star Wars–related questions).
3. Set the **URL** to the **Azure APIM** endpoint.
4. Add the header **`X-API-key`** with your APIM API key value.
5. Click **Add and configure**. Copilot Studio calls APIM → APIM authenticates → calls the MCP Gateway on BTP → the tools are fetched and shown.
6. Ask a question, e.g. *"What can you tell me about Episode 4?"* The agent picks the right tool and returns the film information.

From Copilot Studio you can then publish the agent to **Teams**, **Microsoft 365 Copilot**, **Excel/Word/PowerPoint**, etc.

---

## VS Code — HTTP REST Calls

Save as a `.http` file and run with the VS Code **REST Client** extension. Replace the client ID / secret / URLs with your own values.

```http
@clientid = <your client ID>
@clientsecret = <your client secret>
@MCP-BTP-Server-URL = <your MCP Server URL>
@SAP-BTP-Token-Endpoint = <your token endpoint>

####
# @name login
POST {{SAP-BTP-Token-Endpoint}}
Authorization: Basic {{clientid}}:{{clientsecret}}
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials

####
# @name init
POST {{MCP-BTP-Server-URL}}
Authorization: Bearer {{login.response.body.$.access_token}}
Content-Type: application/json
Accept: application/json, text/event-stream

{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2025-06-18",
    "capabilities": {},
    "clientInfo": { "name": "vscode-rest", "version": "1.0" }
  }
}

####
# @name toolsList
POST {{MCP-BTP-Server-URL}}
Authorization: Bearer {{login.response.body.$.access_token}}
Content-Type: application/json
Accept: application/json, text/event-stream
Mcp-Session-Id: {{init.response.headers.mcp-session-id}}

{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/list"
}
```

> **Notes**
> - The MCP transport requires **both** `application/json` and `text/event-stream` in the `Accept` header.
> - SAP requires an exact `Content-Type: application/json` (no `charset` suffix).
> - The `tools/list` call reuses the `Mcp-Session-Id` returned by the `initialize` response header.
> - The credentials/URLs above are sample values from the demo tenant — use your own.

---

## Azure API Management policy

This inbound policy mints (and caches) a SAP token via `client_credentials`, attaches it as a bearer token, sets the MCP-required headers, strips the APIM subscription key, and routes to the Integration Cell host. Set the named values `sap-token-url`, `sap-client-id`, and `sap-client-secret` in APIM. Make sure to also update the URL to the BTP MCP Gateway. 

```xml
<policies>
    <inbound>
        <base />
        <!-- 1. Reuse a cached SAP token if we have one -->
        <cache-lookup-value key="sap-swapi-token" variable-name="sapAccessToken" />
        <!-- 2. No cached token? Mint one via client_credentials -->
        <choose>
            <when condition="@(!context.Variables.ContainsKey(&quot;sapAccessToken&quot;))">
                <send-request mode="new" response-variable-name="tokenResponse" timeout="20" ignore-error="false">
                    <set-url>{{sap-token-url}}</set-url>
                    <set-method>POST</set-method>
                    <set-header name="Authorization" exists-action="override">
                        <value>@("Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{{sap-client-id}}" + ":" + "{{sap-client-secret}}")))</value>
                    </set-header>
                    <set-header name="Content-Type" exists-action="override">
                        <value>application/x-www-form-urlencoded</value>
                    </set-header>
                    <set-body>grant_type=client_credentials</set-body>
                </send-request>
                <!-- 2a. Fail fast if the token endpoint didn't return 200 -->
                <choose>
                    <when condition="@(((IResponse)context.Variables[&quot;tokenResponse&quot;]).StatusCode != 200)">
                        <return-response>
                            <set-status code="502" reason="SAP token request failed" />
                            <set-header name="Content-Type" exists-action="override">
                                <value>application/json</value>
                            </set-header>
                            <set-body>@("{\"error\":\"sap_token_request_failed\",\"status\":" + ((IResponse)context.Variables["tokenResponse"]).StatusCode + "}")</set-body>
                        </return-response>
                    </when>
                </choose>
                <!-- 2b. Parse token + expiry (read body once) -->
                <set-variable name="tokenJson" value="@(((IResponse)context.Variables[&quot;tokenResponse&quot;]).Body.As<JObject>())" />
                <set-variable name="sapAccessToken" value="@(((JObject)context.Variables[&quot;tokenJson&quot;])[&quot;access_token&quot;].ToString())" />
                <set-variable name="sapTokenTtl" value="@((int)((JObject)context.Variables[&quot;tokenJson&quot;])[&quot;expires_in&quot;])" />
                <!-- 2c. Cache with a 60s safety buffer -->
                <cache-store-value key="sap-swapi-token" value="@((string)context.Variables[&quot;sapAccessToken&quot;])" duration="@(Math.Max(60, (int)context.Variables[&quot;sapTokenTtl&quot;] - 60))" />
            </when>
        </choose>
        <!-- 3. Attach the SAP bearer token -->
        <set-header name="Authorization" exists-action="override">
            <value>@("Bearer " + (string)context.Variables["sapAccessToken"])</value>
        </set-header>
        <!-- 4. MCP transport requires BOTH content types -->
        <set-header name="Accept" exists-action="override">
            <value>application/json, text/event-stream</value>
        </set-header>
        <!-- 4b. SAP requires an exact Content-Type (no charset suffix) -->
        <set-header name="Content-Type" exists-action="override">
            <value>application/json</value>
        </set-header>
        <!-- 5. Don't leak the APIM subscription key to SAP -->
        <set-header name="Ocp-Apim-Subscription-Key" exists-action="delete" />
        <!-- 6. Route to the SAP Integration Cell host -->
        <set-backend-service base-url="https://56587673trial-62ea605b84da4d318d3138da89140509.a.integration.cloud.sap/swapi" />
    </inbound>
    <backend>
        <!-- buffer-response=false keeps the MCP SSE stream flowing -->
        <forward-request buffer-response="false" timeout="120" />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>
```

> **Notes**
> - `buffer-response="false"` keeps the MCP **SSE stream** flowing.
> - The subscription key is deleted before forwarding so it isn't leaked to SAP.
> - Protect the APIM front-end with an API key exposed to Copilot Studio via the `X-API-key` header.

---

## The SWAPI OpenAPI specification

The OpenAPI spec used to generate the MCP tools is included here: [`swapi-openapi-301.yaml`](./swapi-openapi-301.yaml).

**Exposed as tools** (from `https://swapi.info/api`):

| Tag | Operations |
|-----|-----------|
| films | `listFilms`, `getFilm` |
| people | `listPeople`, `getPerson` |
| planets | `listPlanets`, `getPlanet` |
| species | `listSpecies`, `getSpecies` |
| vehicles | `listVehicles`, `getVehicle` |
| starships | `listStarships`, `getStarship` |

> The `*-schema` operations (`getFilmSchema`, etc.) exist in the spec but are intentionally **not** exposed as MCP tools.

---

## References

- Star Wars API — <https://swapi.info/>
- SAP Reference Architecture (MCP Gateway) — <https://architecture.learning.sap.com/docs/ref-arch/d2e34e>
- SAP API Policy (PDF) — <https://help.sap.com/doc/sap-api-policy/latest/en-US/API_Policy_latest.pdf>
- Activate the Integration Cell — <https://help.sap.com/docs/integration-suite/isuite-integrations-and-apis/activate-integration-cell?version=CLOUD>
- Blog: API-centric integration in SAP Integration Suite — a new paradigm — <https://community.sap.com/t5/technology-blog-posts-by-sap/api-centric-integration-in-sap-integration-suite-a-new-paradigm-for-api-amp/ba-p/14438245>
- Blog: MCP Gateway in SAP Integration Suite — your APIs ready for the age of agents — <https://community.sap.com/t5/technology-blog-posts-by-sap/mcp-gateway-in-sap-integration-suite-your-apis-ready-for-the-age-of-agents/ba-p/14438250>
- Blog: API-centric integration — Part 1: Build and deploy — <https://community.sap.com/t5/technology-blog-posts-by-sap/api-centric-integration-on-sap-integration-suite-part-1-build-and-deploy/ba-p/14438357>
- Blog: API-centric integration — Part 2: API governance — <https://community.sap.com/t5/technology-blog-posts-by-sap/api-centric-integration-on-sap-integration-suite-part-2-api-governance-with/ba-p/14438473>

