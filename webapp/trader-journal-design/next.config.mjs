/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "export",
  // В dev сайт по корню (localhost:3000), после экспорта — по /app
  ...(process.env.NODE_ENV === "production" && {
    basePath: "/app",
    assetPrefix: "/app/",
  }),
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
  trailingSlash: true,
}

export default nextConfig
