import { translations } from "@/lib/i18n";
import { describe, expect, it } from "vitest";

describe("translation catalogs", () => {
  it("keeps English and Turkish keys aligned", () => {
    expect(Object.keys(translations.tr).sort()).toEqual(Object.keys(translations.en).sort());
    expect(Object.values(translations.en).every(Boolean)).toBe(true);
    expect(Object.values(translations.tr).every(Boolean)).toBe(true);
  });
});
