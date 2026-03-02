"use client"

import { useEffect, useRef } from "react"

export function HeroIllustration() {
  const canvasRef = useRef<HTMLCanvasElement>(null)

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext("2d")
    if (!ctx) return

    let animId: number
    let frame = 0

    const dpr = window.devicePixelRatio || 1
    const W = 480
    const H = 480
    canvas.width = W * dpr
    canvas.height = H * dpr
    ctx.scale(dpr, dpr)

    const candles = Array.from({ length: 14 }, (_, i) => {
      const open = 140 + Math.sin(i * 0.7) * 60 + Math.random() * 30
      const close = open + (Math.random() - 0.45) * 80
      const high = Math.max(open, close) + Math.random() * 20
      const low = Math.min(open, close) - Math.random() * 20
      return { open, close, high, low, x: 60 + i * 28 }
    })

    const particles: { x: number; y: number; vx: number; vy: number; life: number; maxLife: number }[] = []

    function spawnParticle() {
      particles.push({
        x: Math.random() * W,
        y: Math.random() * H,
        vx: (Math.random() - 0.5) * 0.3,
        vy: (Math.random() - 0.5) * 0.3,
        life: 0,
        maxLife: 120 + Math.random() * 180,
      })
    }

    for (let i = 0; i < 30; i++) spawnParticle()

    function draw() {
      frame++
      ctx!.clearRect(0, 0, W, H)

      for (const p of particles) {
        p.x += p.vx
        p.y += p.vy
        p.life++
        if (p.life > p.maxLife) {
          p.x = Math.random() * W
          p.y = Math.random() * H
          p.life = 0
        }
        const alpha = Math.sin((p.life / p.maxLife) * Math.PI) * 0.4
        ctx!.beginPath()
        ctx!.arc(p.x, p.y, 1.5, 0, Math.PI * 2)
        ctx!.fillStyle = `rgba(139, 92, 246, ${alpha})`
        ctx!.fill()
      }

      ctx!.save()
      ctx!.translate(0, Math.sin(frame * 0.008) * 4)

      const lineY: number[] = []
      for (let i = 0; i < candles.length; i++) {
        const c = candles[i]
        const t = frame * 0.015
        const shift = Math.sin(t + i * 0.5) * 8
        const bullish = c.close < c.open
        const bodyTop = Math.min(c.open, c.close) + shift
        const bodyBot = Math.max(c.open, c.close) + shift
        const wickTop = c.low + shift
        const wickBot = c.high + shift

        const color = bullish
          ? `rgba(139, 92, 246, ${0.5 + Math.sin(t + i) * 0.15})`
          : `rgba(99, 102, 241, ${0.4 + Math.sin(t + i) * 0.1})`

        ctx!.strokeStyle = color
        ctx!.lineWidth = 1.5
        ctx!.beginPath()
        ctx!.moveTo(c.x, wickTop)
        ctx!.lineTo(c.x, wickBot)
        ctx!.stroke()

        const grd = ctx!.createLinearGradient(c.x, bodyTop, c.x, bodyBot)
        if (bullish) {
          grd.addColorStop(0, `rgba(139, 92, 246, 0.6)`)
          grd.addColorStop(1, `rgba(139, 92, 246, 0.2)`)
        } else {
          grd.addColorStop(0, `rgba(99, 102, 241, 0.4)`)
          grd.addColorStop(1, `rgba(99, 102, 241, 0.15)`)
        }
        ctx!.fillStyle = grd
        ctx!.fillRect(c.x - 8, bodyTop, 16, bodyBot - bodyTop)

        if (bullish) {
          ctx!.shadowColor = "rgba(139, 92, 246, 0.3)"
          ctx!.shadowBlur = 12
          ctx!.fillRect(c.x - 8, bodyTop, 16, bodyBot - bodyTop)
          ctx!.shadowBlur = 0
        }

        lineY.push((bodyTop + bodyBot) / 2)
      }

      ctx!.beginPath()
      ctx!.strokeStyle = "rgba(139, 92, 246, 0.3)"
      ctx!.lineWidth = 2
      for (let i = 0; i < lineY.length; i++) {
        const x = candles[i].x
        if (i === 0) ctx!.moveTo(x, lineY[i])
        else {
          const prev = candles[i - 1].x
          const cpx = (prev + x) / 2
          ctx!.bezierCurveTo(cpx, lineY[i - 1], cpx, lineY[i], x, lineY[i])
        }
      }
      ctx!.stroke()

      const last = candles[candles.length - 1]
      const lastY = lineY[lineY.length - 1]
      const pulseR = 4 + Math.sin(frame * 0.05) * 2
      ctx!.beginPath()
      ctx!.arc(last.x, lastY, pulseR, 0, Math.PI * 2)
      ctx!.fillStyle = "rgba(139, 92, 246, 0.8)"
      ctx!.fill()
      ctx!.beginPath()
      ctx!.arc(last.x, lastY, pulseR + 6, 0, Math.PI * 2)
      ctx!.strokeStyle = "rgba(139, 92, 246, 0.2)"
      ctx!.lineWidth = 1
      ctx!.stroke()

      ctx!.restore()

      const cx = W / 2
      const cy = H / 2
      const orbitR = 200
      const orbAlpha = 0.04
      ctx!.beginPath()
      ctx!.arc(cx, cy, orbitR, 0, Math.PI * 2)
      ctx!.strokeStyle = `rgba(139, 92, 246, ${orbAlpha})`
      ctx!.lineWidth = 1
      ctx!.stroke()

      const orbAngle = frame * 0.005
      const dotX = cx + Math.cos(orbAngle) * orbitR
      const dotY = cy + Math.sin(orbAngle) * orbitR
      ctx!.beginPath()
      ctx!.arc(dotX, dotY, 3, 0, Math.PI * 2)
      ctx!.fillStyle = "rgba(139, 92, 246, 0.5)"
      ctx!.fill()

      animId = requestAnimationFrame(draw)
    }

    draw()

    return () => cancelAnimationFrame(animId)
  }, [])

  return (
    <div className="relative w-full max-w-[480px] aspect-square">
      <div className="absolute inset-0 rounded-full bg-accent/[0.06] blur-[80px] animate-morph" />
      <canvas
        ref={canvasRef}
        className="relative w-full h-full"
        style={{ width: 480, height: 480 }}
      />
    </div>
  )
}
