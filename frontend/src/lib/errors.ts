/**
 * Narrows a caught value to a displayable message. Catch clauses are typed
 * `unknown` under `strict`, and anything can be thrown.
 */
export function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof Error && error.message) return error.message;
  if (typeof error === "string" && error) return error;
  return fallback;
}
