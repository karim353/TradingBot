/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "export",
  basePath: "/app",
  assetPrefix: "/app/",
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
  trailingSlash: true,
}

export default nextConfig
