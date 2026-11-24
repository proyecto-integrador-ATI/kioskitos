// One-shot message shown by the next page that reads it, e.g. "client deleted" on the list.
let pending: string | null = null;

export function setNotice(message: string) {
  pending = message;
}

export function takeNotice() {
  const message = pending;
  pending = null;
  return message;
}
