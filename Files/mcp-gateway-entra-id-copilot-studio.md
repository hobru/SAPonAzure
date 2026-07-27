# MCP Gateway on SAP Integration Suite — user authentication with Microsoft Entra ID

Connect **Microsoft Copilot Studio** to the SAP **MCP Gateway** on Integration Suite with **user-based authentication** via **Microsoft Entra ID** — so each end user calls the MCP Gateway with their **own identity** (single sign-on) instead of a shared technical account or API key.

📺 **Watch the video:** [https://www.youtube.com/watch?v=jE-qlg2vZ6I](https://www.youtube.com/watch?v=jE-qlg2vZ6I)

This is the follow-up to *"MCP Gateway on BTP Integration Suite"* ([guide](./mcp-gateway-integration-suite-copilot-studio.md) · [video](https://youtu.be/1m12OVONavA)). The MCP server setup on SAP Integration Suite is the same — same Star Wars API, same exposed tools. What changes here is **how we authenticate**:

| | First video | This video |
|---|---|---|
| Auth to SAP MCP Gateway| `client_credentials` (technical user) | **OAuth 2.0 authorization code** (real user) |
| Intermediary | **Azure API Management** proxy | **Auto-created custom connector** in Copilot Studio |
| Front-end protection | API key (`X-API-key`) | **Entra ID OAuth** connection |
| Identity in MCP Gateway | shared / anonymous | **user context** (UPN + scope), ready to propagate |

> The MCP Gateway is one of two architectures explicitly **endorsed by SAP** in the [SAP API Policy](https://help.sap.com/doc/sap-api-policy/latest/en-US/API_Policy_latest.pdf).

---

## Architecture

```
Copilot Studio  ──▶  Custom connector (auto-created)  ──▶  MCP Gateway (SAP Integration Suite / Integration Cell)  ──▶  SWAPI (Star Wars API)
   (end user)         OAuth2 auth-code towards Entra ID       validates Entra ID JWT (audience + scope sap.access),        (still anonymous
                      + C# script normalizes Content-Type      now holds the USER context (UPN)                             backend call)
```

- **Microsoft Entra ID**: an app registration that exposes the custom scope **`sap.access`** and requests **`offline_access`** (for refresh tokens). It performs the OAuth 2.0 **authorization code** flow.
- **Copilot Studio**: when you add the MCP tool with OAuth, it **auto-creates a custom connector** in the background. That connector holds the OAuth config and runs a small **C# script** to force an exact `Content-Type: application/json`.
- **SAP Integration Suite – MCP Gateway**: the MCP server policy is switched from client-certificate auth to **external authentication**, validating the **Entra ID token** (issuer via `.well-known/openid-configuration`, audience = app ID, scope `sap.access`).
- **Backend (SWAPI)**: still called **anonymously** — but the MCP Gateway now has the **user context** and could propagate it to a real SAP backend in a next step (token exchange).

---

## Prerequisites

- An SAP BTP account with **Integration Suite** provisioned, roles assigned, and the **Integration Cell** enabled. See [Activate the Integration Cell](https://help.sap.com/docs/integration-suite/isuite-integrations-and-apis/activate-integration-cell?version=CLOUD).
- A deployed **MCP Server** artifact on Integration Suite exposing the Star Wars API (see the [first guide](./mcp-gateway-integration-suite-copilot-studio.md) for the build steps and [`swapi-openapi-301.yaml`](./swapi-openapi-301.yaml)).
- A **Microsoft Entra ID** tenant (the same tenant as your Copilot Studio) where you can register an application and grant admin consent.
- **Microsoft Copilot Studio** access.
- VS Code with the **REST Client** extension (to replay and inspect the flow).

---

## Step-by-step

### 1. Register the application in Entra ID
1. In the same Entra tenant as Copilot Studio, go to **Manage → App registrations → New registration**, give it a name, and **Register**.
2. **Certificates & secrets → New client secret** (e.g. *"Copilot Studio to MCP Gateway"*, valid up to 24 months). **Copy the secret value now — it is shown only once.**
3. **Expose an API →** create the **Application ID URI** (keep the default `api://<client-id>`) and **Add a scope** named **`sap.access`** (allow **Admins and users**).
4. **API permissions → Add a permission → My APIs**, select your app, add the **`sap.access`** delegated permission, and **Grant admin consent** so tenant users don't have to consent individually.
5. Still in **API permissions**, add **Microsoft Graph → Delegated → `offline_access`** (so Copilot Studio can obtain a **refresh token**) and **Grant admin consent** again.
6. Note the **Application (client) ID**, **Directory (tenant) ID**, and **Application ID URI** — you'll need them next.

> The **redirect URL** is intentionally left for later — you don't have it yet. Copilot Studio will generate it in step 2.

> Demo values used in the video: tenant `ee9157fe-19b9-4e21-85da-f093ed5df295`, client ID `74612d5d-0ebb-4105-9ba4-4d33a0e73ce9`, scope `api://74612d5d-0ebb-4105-9ba4-4d33a0e73ce9/sap.access offline_access`.

### 2. Add the MCP tool in Copilot Studio (with OAuth)
1. In your agent, disable the tools from the previous video and **add a new tool → Model Context Protocol**. Give it a **name** (e.g. *"SWAPI MCP Gateway with Entra"*) and a **description**.
2. **Server URL**: the URL of your **MCP server on Integration Suite** — this time we call the SAP MCP Gateway **directly** (no APIM proxy).
3. **Authentication**: choose **OAuth** (not API key) and **Manual**, then enter the properties from your app registration:
   - **Client ID**: the Application (client) ID.
   - **Client secret**: the value you saved in step 1.
   - **Authorization URL**: `https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/authorize`
   - **Token URL**: `https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/token`
   - **Refresh URL**: same as the Token URL.
   - **Scope**: `api://<client-id>/sap.access offline_access`
4. Click **Create**. Copilot Studio now generates a **redirect URL** — copy it.

### 3. Add the redirect URL back in Entra ID
1. Return to your **app registration → Authentication → Add a platform → Web**.
2. Paste the **redirect URL** from Copilot Studio and **Configure/Save**.

The Microsoft side is now complete.

### 4. Switch the SAP MCP policy to external authentication
On the MCP server's **Policies**, change from the default client-certificate auth to **external authentication** so that **Entra ID issues** the token and the **MCP Gateway validates** it.

**Authentication step:**
- **Well-known URL** (OpenID configuration): `https://login.microsoftonline.com/<tenant-id>/v2.0/.well-known/openid-configuration` (open it in a browser to confirm it resolves).
- **Audience**: your **Application ID / client ID** (`api://<client-id>` / the client ID).
- **Client ID key**: read from the incoming token — `${authn.oidc.jwt.appid}`.
- **User info key**: read from the incoming token — `${authn.oidc.jwt.upn}`.

**Save and deploy.**

> At this point authentication works but **authorization** is not configured yet. A first test from Copilot Studio returns *"user does not have authorizations"* — which confirms the token was **authenticated** and only the scope check is missing.

### 5. Configure the authorization (scope check)
1. On the policy's **Authorization** tab → **Policy settings**:
   - **Scope key**: `SCP` (the claim Entra ID sends).
   - **Scope**: `sap.access` (the scope you defined in Entra ID).
2. **Save and deploy.**

### 6. Fix the Content-Type in the auto-created custom connector
A refreshed connection in Copilot Studio now fails with *"content type must be application/json"*. Copilot Studio sends `application/json; charset=utf-8`, but the SAP MCP handler does a **strict equality check** and accepts **only** `application/json`.
1. Find the **custom connector** that Copilot Studio created in the background when you set up the connection (**Power Automate → Custom connectors**).
2. **Edit → Code** view: enable the code and replace the script with [`custom-connector-script.csx`](./custom-connector-script.csx), which sets the outgoing `Content-Type` to exactly `application/json`.
3. **Update connector.** Allow a minute or so for the change to replicate/cache.

### 7. Verify in Copilot Studio
1. Back in Copilot Studio, **refresh** the connection — the **list of tools** now appears.
2. On first connect you get an **Entra ID sign-in pop-up**; each end user authenticates with **their own account** (single sign-on).
3. Ask a question, e.g. *"What can you tell me about episode 5?"* The agent picks the right tool (`getFilm`) and returns the film — calling the MCP Gateway **in the signed-in user's context**.

### 8. (Optional) Replay the OAuth flow in VS Code to inspect the token
To see exactly what Copilot Studio does behind the scenes, reproduce the flow with the REST Client using [`entra-id-auth.http`](./entra-id-auth.http):
1. Add a **`http://localhost:8080/callback`** redirect URL to the Entra app (so the token/code is returned to a browser you control).
2. Open the **authorize** URL in a browser, sign in, and copy the `?code=` value.
3. **Exchange the code** for an `access_token` at the token endpoint.
4. Decode the JWT: confirm your **`upn`/email** (user context) and **`scp` = `sap.access`** — exactly the scope enforced by the MCP server's authorization step.

---

## Next steps — end-to-end user propagation

The MCP Gateway now holds a **validated user token**: the user, the authentication, and the `sap.access` scope. The natural next step is to **forward this token** to the backend system — or perform a **token exchange** — so you get a full **end-to-end principal propagation** flow into a real SAP backend (in this demo the SWAPI backend is still called anonymously).

---

## VS Code — Entra ID OAuth + MCP calls

Full snippet: [`entra-id-auth.http`](./entra-id-auth.http). Highlights:

```http
@tenant = <your tenant id>
@clientId = <your client id>
@clientSecret = <your client secret>
@redirectUri = http://localhost:8080/callback
@scope = api://<your client id>/sap.access offline_access

### 1. Authorize (open in a browser, copy the ?code= from the callback)
# https://login.microsoftonline.com/{{tenant}}/oauth2/v2.0/authorize
#   ?client_id={{clientId}}&response_type=code
#   &redirect_uri={{redirectUri}}
#   &scope=api://<client-id>/sap.access%20offline_access&response_mode=query

### 2. Exchange the code for a token
# @name token
POST https://login.microsoftonline.com/{{tenant}}/oauth2/v2.0/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code
&client_id={{clientId}}
&client_secret={{clientSecret}}
&redirect_uri={{redirectUri}}
&scope={{scope}}
&code=<paste the fresh ?code= value here>
```

> **Notes**
> - The MCP transport requires **both** `application/json` and `text/event-stream` in the `Accept` header.
> - SAP requires an exact `Content-Type: application/json` (no `charset` suffix) — that's what the custom-connector C# script enforces.
> - The authorization `code` is **single-use** and expires in ~10 minutes; use the `refresh_token` (from `offline_access`) to renew the access token.
> - The `tools/list` call reuses the `Mcp-Session-Id` returned by the `initialize` response header.
> - Never commit the real client secret or authorization codes.

---

## Custom connector — C# script

The custom connector that Copilot Studio auto-creates runs this **C# script** so the outgoing request carries an exact `Content-Type: application/json`. See [`custom-connector-script.csx`](./custom-connector-script.csx).

```csharp
public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        var request = this.Context.Request;

        // Force Content-Type to exactly "application/json" (SAP MCP handler
        // rejects "application/json; charset=utf-8" with a strict equality check)
        if (request.Content != null)
        {
            request.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/json");
        }

        // Forward to the SAP MCP Gateway unchanged (Authorization + Accept stay intact)
        HttpResponseMessage response =
            await this.Context.SendAsync(request, this.CancellationToken).ConfigureAwait(false);

        return response;
    }
}
```

> **Notes**
> - The script leaves the **Authorization** (Entra ID bearer) and **Accept** headers untouched.
> - It only normalizes `Content-Type`; the OAuth flow is handled by the connector's security configuration.

---

## References

- SAP Reference Architecture (MCP Gateway) — <https://architecture.learning.sap.com/docs/ref-arch/d2e34e>
- SAP API Policy (PDF) — <https://help.sap.com/doc/sap-api-policy/latest/en-US/API_Policy_latest.pdf>
- Activate the Integration Cell — <https://help.sap.com/docs/integration-suite/isuite-integrations-and-apis/activate-integration-cell?version=CLOUD>
- Microsoft identity platform — Authorization code flow — <https://learn.microsoft.com/entra/identity-platform/v2-oauth2-auth-code-flow>
- Expose an API and add a scope in Entra ID — <https://learn.microsoft.com/entra/identity-platform/quickstart-configure-app-expose-web-apis>
- Write code in a custom connector — <https://learn.microsoft.com/connectors/custom-connectors/write-code>
- OpenID Connect discovery (`.well-known/openid-configuration`) — <https://learn.microsoft.com/entra/identity-platform/v2-protocols-oidc>
- First guide — MCP Gateway on SAP Integration Suite — [`mcp-gateway-integration-suite-copilot-studio.md`](./mcp-gateway-integration-suite-copilot-studio.md)
- Star Wars API — <https://swapi.info/>
