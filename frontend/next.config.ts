import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // The Dockerfile runs `node server.js` from .next/standalone, which only exists
  // when this output mode is set.
  output: "standalone",
};

export default nextConfig;
