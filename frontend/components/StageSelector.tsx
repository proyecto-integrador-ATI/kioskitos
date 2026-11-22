"use client";

import { useState } from "react";

type StageSelectorProps = {
  /** Names of the user's existing stage names. */
  available: string[];
  /** Stage names of the project, in order. */
  value: string[];
  onChange: (value: string[]) => void;
};

const sameName = (a: string, b: string) => a.toLowerCase() === b.toLowerCase();

export function StageSelector({ available, value, onChange }: StageSelectorProps) {
  const [selected, setSelected] = useState("");
  const [newStage, setNewStage] = useState("");

  const isAdded = (name: string) => value.some((v) => sameName(v, name));
  const options = available.filter((name) => !isAdded(name));

  function add(name: string) {
    const trimmed = name.trim();
    if (trimmed && !isAdded(trimmed)) {
      // Reuse the existing spelling if the name already exists ("diseño" → "Diseño").
      onChange([...value, available.find((a) => sameName(a, trimmed)) ?? trimmed]);
    }
  }

  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="text-sm font-medium">Etapas</legend>

      {value.length > 0 && (
        <ol className="flex list-decimal flex-col gap-1 pl-6 text-sm">
          {value.map((name) => (
            <li key={name}>
              {name}{" "}
              <button
                type="button"
                onClick={() => onChange(value.filter((v) => v !== name))}
                aria-label={`Quitar ${name}`}
                className="ml-2 text-slate-500 hover:underline"
              >
                Quitar
              </button>
            </li>
          ))}
        </ol>
      )}

      {options.length > 0 && (
        <div className="flex gap-2">
          <select
            aria-label="Etapa existente"
            value={selected}
            onChange={(e) => setSelected(e.target.value)}
            className="rounded-md border border-slate-300 px-2 py-1 text-sm"
          >
            <option value="">Elegí una etapa…</option>
            {options.map((name) => (
              <option key={name} value={name}>
                {name}
              </option>
            ))}
          </select>
          <button
            type="button"
            disabled={!selected}
            onClick={() => {
              add(selected);
              setSelected("");
            }}
            className="rounded-md border border-slate-300 px-3 py-1 text-sm hover:bg-slate-50 disabled:opacity-50"
          >
            Agregar etapa
          </button>
        </div>
      )}

      <div className="flex gap-2">
        <input
          aria-label="Nueva etapa"
          placeholder="Nueva etapa"
          value={newStage}
          maxLength={100}
          onChange={(e) => setNewStage(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault(); // do not submit the form
              add(newStage);
              setNewStage("");
            }
          }}
          className="rounded-md border border-slate-300 px-2 py-1 text-sm"
        />
        <button
          type="button"
          onClick={() => {
            add(newStage);
            setNewStage("");
          }}
          className="rounded-md border border-slate-300 px-3 py-1 text-sm hover:bg-slate-50"
        >
          Crear etapa
        </button>
      </div>
    </fieldset>
  );
}
