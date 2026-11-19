/** ProblemDetails body returned by the backend on errors. */
export type ProblemDetails = {
  title?: string;
  detail?: string;
  status?: number;
  errorCode?: string;
};

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly problem: ProblemDetails | null,
  ) {
    super(`The API responded with the HTTP status ${status}.`);
    this.name = "ApiError";
  }
}
