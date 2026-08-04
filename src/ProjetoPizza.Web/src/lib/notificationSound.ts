type NotificationTone = 'confirmation' | 'order' | 'service-call'

let audioContext: AudioContext | undefined

export async function unlockNotificationSound(): Promise<boolean> {
  const context = getAudioContext()
  if (!context) return false
  if (context.state === 'suspended') await context.resume().catch(() => undefined)
  return context.state === 'running'
}

export async function playNotificationTone(tone: NotificationTone = 'confirmation'): Promise<boolean> {
  if (!await unlockNotificationSound()) return false
  const context = getAudioContext()
  if (!context) return false

  const frequencies = tone === 'order'
    ? [880, 1174]
    : tone === 'service-call'
      ? [740]
      : [660, 880]
  const startAt = context.currentTime + 0.01

  frequencies.forEach((frequency, index) => {
    const oscillator = context.createOscillator()
    const gain = context.createGain()
    const toneStart = startAt + index * 0.14
    const toneEnd = toneStart + 0.2

    oscillator.type = 'sine'
    oscillator.frequency.setValueAtTime(frequency, toneStart)
    gain.gain.setValueAtTime(0.0001, toneStart)
    gain.gain.exponentialRampToValueAtTime(0.13, toneStart + 0.02)
    gain.gain.exponentialRampToValueAtTime(0.0001, toneEnd)
    oscillator.connect(gain)
    gain.connect(context.destination)
    oscillator.start(toneStart)
    oscillator.stop(toneEnd)
  })

  return true
}

function getAudioContext(): AudioContext | undefined {
  if (audioContext) return audioContext
  const AudioContextClass = window.AudioContext ?? window.webkitAudioContext
  if (!AudioContextClass) return undefined

  try {
    audioContext = new AudioContextClass()
    return audioContext
  } catch {
    return undefined
  }
}

declare global {
  interface Window {
    webkitAudioContext?: typeof AudioContext
  }
}
