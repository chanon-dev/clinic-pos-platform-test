const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5001";

interface ApiOptions {
  method?: string;
  body?: unknown;
  token?: string;
}

export async function api<T>(path: string, options: ApiOptions = {}): Promise<T> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (options.token) {
    headers["Authorization"] = `Bearer ${options.token}`;
  }

  const res = await fetch(`${API_URL}${path}`, {
    method: options.method || "GET",
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined,
  });

  if (!res.ok) {
    const error = await res.json().catch(() => ({ detail: "An error occurred" }));
    throw { status: res.status, ...error };
  }

  return res.json();
}
