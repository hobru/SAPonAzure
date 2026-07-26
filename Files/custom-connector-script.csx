// Power Automate / Copilot Studio Custom Connector — code (C#) script.
//
// Paste this into the "Code" step of the custom connector (Definition -> Code)
// and make sure the operations you want it to run on are selected.
//
// Purpose: the SAP MCP handler does a *strict* equality check on the
// Content-Type header and rejects "application/json; charset=utf-8".
// This script rewrites it to exactly "application/json" before forwarding,
// while leaving the Authorization and Accept headers intact.

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
