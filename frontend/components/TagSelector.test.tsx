import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";

import { TagSelector } from "./TagSelector";

function Harness({ available, initial = [] }: { available: string[]; initial?: string[] }) {
  const [value, setValue] = useState(initial);
  return (
    <>
      <TagSelector available={available} value={value} onChange={setValue} />
      <output>{value.join("|")}</output>
    </>
  );
}

describe("TagSelector", () => {
  it("selects and unselects existing tags", async () => {
    render(<Harness available={["Branding", "Web"]} />);

    await userEvent.click(screen.getByLabelText("Web"));
    expect(screen.getByRole("status")).toHaveTextContent("Web");

    await userEvent.click(screen.getByLabelText("Web"));
    expect(screen.getByRole("status")).toBeEmptyDOMElement();
  });

  it("adds a new tag with Enter and shows it selected", async () => {
    render(<Harness available={["Branding"]} />);

    await userEvent.type(screen.getByLabelText("Nueva etiqueta"), " Diseño {Enter}");

    expect(screen.getByRole("status")).toHaveTextContent("Diseño");
    expect(screen.getByLabelText("Diseño")).toBeChecked();
    expect(screen.getByLabelText("Nueva etiqueta")).toHaveValue("");
  });

  it("reuses the existing spelling and ignores duplicates regardless of case", async () => {
    render(<Harness available={["Web"]} />);
    const input = screen.getByLabelText("Nueva etiqueta");

    await userEvent.type(input, "web{Enter}");
    await userEvent.type(input, "WEB{Enter}");

    expect(screen.getByRole("status")).toHaveTextContent(/^Web$/);
  });
});
