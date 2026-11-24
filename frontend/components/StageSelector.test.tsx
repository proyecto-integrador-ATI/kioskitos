import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";

import { StageSelector } from "./StageSelector";

function Harness({ available }: { available: string[] }) {
  const [value, setValue] = useState<string[]>([]);
  return (
    <>
      <StageSelector available={available} value={value} onChange={setValue} />
      <output>{value.join("|")}</output>
    </>
  );
}

describe("StageSelector", () => {
  it("adds existing stages from the dropdown and removes them from the options", async () => {
    render(<Harness available={["Desarrollo", "Diseño"]} />);
    const dropdown = screen.getByLabelText("Etapa existente");

    await userEvent.selectOptions(dropdown, "Diseño");
    await userEvent.click(screen.getByRole("button", { name: "Agregar etapa" }));

    expect(screen.getByRole("status")).toHaveTextContent("Diseño");
    expect(within(dropdown).queryByRole("option", { name: "Diseño" })).not.toBeInTheDocument();
  });

  it("creates a new stage and keeps the order", async () => {
    render(<Harness available={["Diseño"]} />);

    await userEvent.selectOptions(screen.getByLabelText("Etapa existente"), "Diseño");
    await userEvent.click(screen.getByRole("button", { name: "Agregar etapa" }));
    await userEvent.type(screen.getByLabelText("Nueva etapa"), " Testing {Enter}");

    expect(screen.getByRole("status")).toHaveTextContent(/^Diseño\|Testing$/);
    expect(screen.getByLabelText("Nueva etapa")).toHaveValue("");
  });

  it("reuses the existing spelling and ignores repeated names", async () => {
    render(<Harness available={["Diseño"]} />);
    const input = screen.getByLabelText("Nueva etapa");

    await userEvent.type(input, "diseño{Enter}");
    await userEvent.type(input, "DISEÑO{Enter}");

    expect(screen.getByRole("status")).toHaveTextContent(/^Diseño$/);
  });

  it("removes a stage", async () => {
    render(<Harness available={[]} />);

    await userEvent.type(screen.getByLabelText("Nueva etapa"), "Diseño{Enter}");
    await userEvent.click(screen.getByRole("button", { name: "Quitar Diseño" }));

    expect(screen.getByRole("status")).toBeEmptyDOMElement();
  });
});
