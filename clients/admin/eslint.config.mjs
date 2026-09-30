import { defineConfig, globalIgnores } from "eslint/config";
import nextVitals from "eslint-config-next/core-web-vitals";
import nextTs from "eslint-config-next/typescript";

const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTs,
  // Override default ignores of eslint-config-next.
  globalIgnores([
    // Default ignores of eslint-config-next:
    ".next/**",
    "out/**",
    "build/**",
    "next-env.d.ts",
  ]),
  {
    // @floating-ui/react's API is built entirely around a `context`/`refs`
    // object that is read and passed around throughout render by design —
    // that's how the library composes positioning + interaction hooks. The
    // React Compiler's react-hooks/refs rule can't distinguish that from an
    // actual `.current` read-during-render hazard and flags every such
    // usage, so it's disabled for the component library built on it.
    files: [
      "src/components/foundation/**/*.{ts,tsx}",
      "src/components/select/**/*.tsx",
      "src/components/command/**/*.tsx",
    ],
    rules: {
      "react-hooks/refs": "off",
    },
  },
]);

export default eslintConfig;
