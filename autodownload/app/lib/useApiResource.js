"use client";
import { useCallback, useEffect, useRef, useState } from "react";
import { apiRequest } from "./apiClient";

export function useApiResource(path, fallbackValue, options = {}) {
  const fallbackRef = useRef(fallbackValue);
  const [data, setData] = useState(fallbackValue);
  const [loading, setLoading] = useState(Boolean(path));
  const [error, setError] = useState("");
  const [usingFallback, setUsingFallback] = useState(false);

  useEffect(() => {
    fallbackRef.current = fallbackValue;
  }, [fallbackValue]);

  const load = useCallback(async () => {
    if (!path) return;

    setLoading(true);
    setError("");

    try {
      const payload = await apiRequest(path, options);
      setData(payload);
      setUsingFallback(false);
    } catch (err) {
      setError(err.message || "API indisponível.");
      setData(fallbackRef.current);
      setUsingFallback(true);
    } finally {
      setLoading(false);
    }
  }, [path, options.auth]);

  useEffect(() => {
    let active = true;

    async function run() {
      if (!path) return;
      setLoading(true);
      setError("");

      try {
        const payload = await apiRequest(path, options);
        if (!active) return;
        setData(payload);
        setUsingFallback(false);
      } catch (err) {
        if (!active) return;
        setError(err.message || "API indisponível.");
        setData(fallbackRef.current);
        setUsingFallback(true);
      } finally {
        if (active) setLoading(false);
      }
    }

    run();
    return () => {
      active = false;
    };
  }, [path, options.auth]);

  return { data, setData, loading, error, usingFallback, reload: load };
}
