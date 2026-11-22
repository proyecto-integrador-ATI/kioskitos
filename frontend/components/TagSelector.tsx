"use client";

import { useState } from "react";

type TagSelectorProps = {
  /** Names of the user's existing tags. */
  available: string[];
  /** Selected tag names (existing or new). */
  value: string[];
  onChange: (value: string[]) => void;
};

const sameName = (a: string, b: string) => a.toLowerCase() === b.toLowerCase();

export function TagSelector({ available, value, onChange }: TagSelectorProps) {
  const [newTag, setNewTag] = useState("");

  const isSelected = (name: string) => value.some((v) => sameName(v, name));

  function toggle(name: string) {
    onChange(isSelected(name) ? value.filter((v) => !sameName(v, name)) : [...value, name]);
  }

  function add() {
    const name = newTag.trim();
    if (name && !isSelected(name)) {
      // Reuse the existing spelling if the tag already exists ("web" → "Web").
      onChange([...value, available.find((a) => sameName(a, name)) ?? name]);
    }
    setNewTag("");
  }

  const newOnes = value.filter((v) => !available.some((a) => sameName(a, v)));

  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="text-sm font-medium">Etiquetas</legend>

      <div className="flex flex-wrap gap-3">
        {[...available, ...newOnes].map((name) => (
          <label key={name} className="flex items-center gap-1 text-sm">
            <input type="checkbox" checked={isSelected(name)} onChange={() => toggle(name)} />
            {name}
          </label>
        ))}
      </div>

      <div className="flex gap-2">
        <input
          aria-label="Nueva etiqueta"
          value={newTag}
          maxLength={50}
          onChange={(e) => setNewTag(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault(); // do not submit the form
              add();
            }
          }}
          className="rounded-md border border-slate-300 px-2 py-1 text-sm"
        />
        <button
          type="button"
          onClick={add}
          className="rounded-md border border-slate-300 px-3 py-1 text-sm hover:bg-slate-50"
        >
          Agregar
        </button>
      </div>
    </fieldset>
  );
}
