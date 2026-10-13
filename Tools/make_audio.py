#!/usr/bin/env python3
"""Synthesise the game's placeholder sound effects as 16-bit mono WAV files.

Every sound under Assets/_Project/Audio comes from this script, so none of them needs a licence.
Pure standard library:

    python3 Tools/make_audio.py            # writes into Assets/_Project/Audio
    python3 Tools/make_audio.py some/dir   # writes somewhere else
"""
import math, os, random, struct, sys, wave

RATE = 44100
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Project", "Audio")


def write(path, samples, peak=0.9):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    top = max(1e-9, max(abs(s) for s in samples))
    scale = peak / top
    with wave.open(path, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s * scale)) * 32767)) for s in samples))
    print(f"{os.path.relpath(path, OUT):40s} {len(samples) / RATE:5.2f}s")


def lowpass(samples, cutoff):
    a = 1.0 - math.exp(-2.0 * math.pi * cutoff / RATE)
    y, out = 0.0, []
    for s in samples:
        y += a * (s - y)
        out.append(y)
    return out


def highpass(samples, cutoff):
    low = lowpass(samples, cutoff)
    return [s - l for s, l in zip(samples, low)]


def noise(n, rng):
    return [rng.uniform(-1.0, 1.0) for _ in range(n)]


def env(n, attack, decay):
    a = max(1, int(attack * RATE))
    return [min(1.0, i / a) * math.exp(-max(0, i - a) / (decay * RATE)) for i in range(n)]


def mix(*tracks):
    n = max(len(t) for t in tracks)
    return [sum(t[i] if i < len(t) else 0.0 for t in tracks) for i in range(n)]


def gain(samples, g):
    return [s * g for s in samples]


def clip(samples, drive):
    return [math.tanh(s * drive) for s in samples]


def fade_out(samples, seconds):
    n = min(len(samples), int(seconds * RATE))
    out = list(samples)
    for i in range(n):
        out[len(out) - 1 - i] *= i / n
    return out


def sweep(n, f0, f1, decay):
    out, phase = [], 0.0
    for i in range(n):
        t = i / n
        f = f0 + (f1 - f0) * t
        phase += 2.0 * math.pi * f / RATE
        out.append(math.sin(phase) * math.exp(-i / (decay * RATE)))
    return out


def tone(n, freq, decay, harmonics=((1.0, 1.0),)):
    out = []
    for i in range(n):
        t = i / RATE
        s = sum(a * math.sin(2.0 * math.pi * freq * h * t) for h, a in harmonics)
        out.append(s * math.exp(-i / (decay * RATE)))
    return out


def gunshot(seed, length, crack_cut, crack_decay, body_cut, body_decay, thump, thump_decay, drive):
    rng = random.Random(seed)
    n = int(length * RATE)
    crack = [a * b for a, b in zip(highpass(lowpass(noise(n, rng), crack_cut), 900), env(n, 0.0004, crack_decay))]
    body = [a * b for a, b in zip(lowpass(noise(n, rng), body_cut), env(n, 0.001, body_decay))]
    low = sweep(n, thump, thump * 0.4, thump_decay)
    tail = [a * b for a, b in zip(lowpass(noise(n, rng), 1400), env(n, 0.02, length * 0.35))]
    return fade_out(clip(mix(gain(crack, 1.0), gain(body, 1.3), gain(low, 1.1), gain(tail, 0.25)), drive), 0.05)


def click(seed, length, freq, decay, noise_amount):
    rng = random.Random(seed)
    n = int(length * RATE)
    tick = [a * b for a, b in zip(highpass(noise(n, rng), 2500), env(n, 0.0003, decay * 0.4))]
    ring = tone(n, freq, decay, ((1.0, 1.0), (2.7, 0.4)))
    return fade_out(mix(gain(tick, noise_amount), ring), 0.01)


def loop(samples, crossfade):
    """Crossfade the tail into the head so the clip loops without a click."""
    n = int(crossfade * RATE)
    body = samples[n:]
    for i in range(n):
        t = i / n
        body[len(body) - n + i] = body[len(body) - n + i] * (1.0 - t) + samples[i] * t
    return body


W = os.path.join(OUT, "Weapons")
# Pistol: a short pop with some weight. SMG: short and snappy. Rifle: sharper with more body.
# Shotgun: big low boom. Sniper: the loudest, long tail.
for i in (1, 2):
    write(f"{W}/Shot_M1911_{i}.wav", gunshot(50 + i, 0.36, 4800, 0.02, 1000, 0.042, 125, 0.04, 2.4))
    write(f"{W}/Shot_Uzi_{i}.wav", gunshot(10 + i, 0.32, 5200, 0.018, 1500, 0.035, 170, 0.03, 2.2))
    write(f"{W}/Shot_AK74_{i}.wav", gunshot(20 + i, 0.45, 6500, 0.022, 1300, 0.05, 150, 0.045, 2.6))
    write(f"{W}/Shot_Benelli_{i}.wav", gunshot(30 + i, 0.7, 3800, 0.04, 800, 0.11, 95, 0.09, 3.0))
    write(f"{W}/Shot_M107_{i}.wav", gunshot(40 + i, 1.1, 7000, 0.03, 700, 0.16, 70, 0.14, 3.4))

write(f"{W}/DryFire.wav", click(1, 0.09, 2100, 0.012, 0.9), 0.6)
write(f"{W}/MagazineOut.wav", mix(click(2, 0.16, 900, 0.03, 0.7), [0.0] * int(0.05 * RATE) + click(3, 0.1, 1500, 0.015, 0.5)), 0.7)
write(f"{W}/MagazineIn.wav", mix(click(4, 0.18, 700, 0.035, 0.8), [0.0] * int(0.06 * RATE) + click(5, 0.1, 1900, 0.012, 0.7)), 0.75)
write(f"{W}/SlideRelease.wav", mix(click(6, 0.2, 1300, 0.03, 1.0), [0.0] * int(0.035 * RATE) + click(7, 0.12, 2600, 0.02, 0.8)), 0.8)

R = os.path.join(OUT, "Range")
n = int(1.1 * RATE)
write(f"{R}/SteelPing.wav", fade_out(mix(tone(n, 1850, 0.22, ((1.0, 1.0), (2.41, 0.55), (3.86, 0.3), (5.3, 0.15))), click(8, 0.05, 4000, 0.006, 1.0)), 0.1), 0.8)
rng = random.Random(9)
n = int(0.12 * RATE)
write(f"{R}/PaperThwack.wav", fade_out([a * b for a, b in zip(highpass(lowpass(noise(n, rng), 3200), 500), env(n, 0.0005, 0.016))], 0.02), 0.55)
n = int(0.2 * RATE)
write(f"{R}/WoodThud.wav", fade_out(mix(sweep(n, 260, 140, 0.03), gain([a * b for a, b in zip(lowpass(noise(n, rng), 1800), env(n, 0.0005, 0.02))], 0.6)), 0.03), 0.6)
write(f"{R}/DirtThud.wav", fade_out(mix(gain(sweep(n, 130, 70, 0.04), 0.8), [a * b for a, b in zip(lowpass(noise(n, rng), 700), env(n, 0.002, 0.045))]), 0.03), 0.45)
write(f"{R}/HitTick.wav", click(10, 0.06, 3100, 0.012, 0.3), 0.5)
n = int(0.16 * RATE)
write(f"{R}/CountdownBeep.wav", fade_out([s * min(1.0, i / 200) for i, s in enumerate(tone(n, 880, 10.0))], 0.03), 0.5)
n = int(0.7 * RATE)
square = [1.0 if math.sin(2 * math.pi * 440 * i / RATE) >= 0 else -1.0 for i in range(n)]
write(f"{R}/StartBuzzer.wav", fade_out([s * min(1.0, i / 300) for i, s in enumerate(lowpass(square, 2400))], 0.06), 0.5)
n = int(1.0 * RATE)
square = [1.0 if math.sin(2 * math.pi * 300 * i / RATE) >= 0 else -1.0 for i in range(n)]
write(f"{R}/EndBuzzer.wav", fade_out([s * min(1.0, i / 300) for i, s in enumerate(lowpass(square, 1800))], 0.12), 0.5)
n = int(0.35 * RATE)
square = [1.0 if math.sin(2 * math.pi * 150 * i / RATE) >= 0 else -1.0 for i in range(n)]
write(f"{R}/PenaltyBuzzer.wav", fade_out([s * min(1.0, i / 200) for i, s in enumerate(lowpass(square, 1200))], 0.05), 0.5)
write(f"{R}/ButtonClick.wav", click(11, 0.07, 1500, 0.01, 0.8), 0.5)

# Carrier motor: a steady hum with a little grit, made loopable.
n = int(1.4 * RATE)
rng = random.Random(12)
hum = [0.6 * math.sin(2 * math.pi * 92 * i / RATE) + 0.3 * math.sin(2 * math.pi * 184 * i / RATE) + 0.18 * math.sin(2 * math.pi * 276 * i / RATE) for i in range(n)]
write(f"{R}/CarrierMotor.wav", loop(mix(hum, gain(lowpass(noise(n, rng), 900), 0.25)), 0.4), 0.5)

# Outdoor ambience: slow wind made from low-passed noise with a drifting level, loopable.
n = int(9.0 * RATE)
rng = random.Random(13)
base = lowpass(lowpass(noise(n, rng), 520), 520)
wind = [s * (0.55 + 0.3 * math.sin(2 * math.pi * i / (4.5 * RATE)) + 0.15 * math.sin(2 * math.pi * i / (1.7 * RATE) + 1.3)) for i, s in enumerate(base)]
write(os.path.join(OUT, "Ambience", "WindLoop.wav"), loop(wind, 1.5), 0.35)
