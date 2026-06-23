## Eyeshot (devDept)

This project uses the devDept **Eyeshot** geometry/visualization library.

Rules:
- **Whenever you need information about Eyeshot (APIs, signatures, behavior, capabilities), consult the Eyeshot MCP FIRST** (server `eyeshot`, configured in `.mcp.json` / `.vscode/mcp.json`, url `https://devdept.com/mcp`). Prefer it over web search or guessing.
- If the `eyeshot` MCP tools are not available in the session, say so, and fall back to the bundled API docs shipped with the package (`~/.nuget/packages/devdept.eyeshot/<version>/lib/net8.0/devDept.Eyeshot.v2025.xml`) — the authoritative offline reference.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
