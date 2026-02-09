const fs = require("fs")
const path = require("path")

const src = path.join(__dirname, "..", "out")
const dest = path.join(__dirname, "..", "..", "..", "TradingBot", "wwwroot", "app")

if (!fs.existsSync(src)) {
  console.error("out folder not found. Run next build first.")
  process.exit(1)
}

if (fs.existsSync(dest)) {
  fs.rmSync(dest, { recursive: true })
}

fs.cpSync(src, dest, { recursive: true })
console.log("Copied to TradingBot/wwwroot/app")
