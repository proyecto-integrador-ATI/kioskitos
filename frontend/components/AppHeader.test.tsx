import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { AppHeader } from "./AppHeader";

describe("AppHeader", () => {
  it.each([
    ["Clientes", "/clients"],
    ["Proyectos", "/projects"],
    ["Mi perfil", "/profile"],
  ])("links %s to %s", (name, href) => {
    render(<AppHeader />);

    expect(screen.getByRole("link", { name })).toHaveAttribute("href", href);
  });
});
