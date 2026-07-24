# Architecture Decision Records (ADR Log)

## ADR-001: Target .NET 10 LTS Framework
- **Status**: Approved
- **Context**: Project started in 2026. .NET 10 is the latest Long Term Support (LTS) release supported until 2028.
- **Decision**: Target `net10.0` for all backend projects (`Domain`, `Application`, `Infrastructure`, `API`).

## ADR-002: MediatR Versioning [12.4.0, 13.0.0)
- **Status**: Approved
- **Context**: MediatR shifted to commercial licensing starting in v13.
- **Decision**: Pin MediatR dependency to range `[12.4.0, 13.0.0)` which represents the final MIT-licensed stable 12.x releases.

## ADR-003: Denormalized Multi-Tenancy & EF Core Global Filters
- **Status**: Approved
- **Context**: Prevent cross-tenant data leakage across all tables without requiring complex multi-table joins.
- **Decision**: Add denormalized `TenantId` column to all tenant-scoped entities implementing `IMultiTenant`. Apply automatic EF Core Global Query Filter `x => !x.IsDeleted && x.TenantId == CurrentTenantId`.

## ADR-004: Idempotent Quota Ledger via QuotaReservations
- **Status**: Approved
- **Context**: Atomically reserve tokens, handle Hangfire retries idempotently, and refund failed jobs cleanly.
- **Decision**: Create a `QuotaReservations` ledger table linked by unique `JobId`. Handle state transitions (`Reserved` -> `Committed` / `Refunded`).

## ADR-005: Provider-Native GEO Citation Adapters
- **Status**: Approved
- **Context**: LLM providers return structured citation metadata natively.
- **Decision**: Implement `IGeoEngineClient` adapters for Perplexity Sonar (`citations`), OpenAI (`web_search`), Gemini (`groundingMetadata`), and Anthropic. Use 3-5 sample iterations with jitter for Mention Rate % calculations.

## ADR-006: URL Verification & Gold Opportunity Trigger
- **Status**: Approved
- **Context**: LLM may cite domain pages that do not exist (404), representing prime GEO opportunity.
- **Decision**: Execute HTTP HEAD/GET validation. Classify as `Valid`, `NonExistentPage`, `WrongDomain`, or `Unreachable`. If `NonExistentPage`, trigger actionable Gold Opportunity notification.
