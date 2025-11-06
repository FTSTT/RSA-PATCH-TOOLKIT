[中文](README.md)

# RSA PATCH TOOLKIT

> 🧠 Parts of the software implementation and this documentation were assisted by **AI**, to improve development and writing efficiency.  
> This toolkit is for **nibble / 1‑byte / fixed CRC32 patch assistance**. It is **not intended for big‑integer factoring** — just for fun.

---

## 🌟 Features

- Supports **HEX / Decimal / Base64** format detection and conversion  
- Provides both **Byte‑Tweak Mode** and **Fixed CRC32 Mode**
- **Half‑nibble replacement** works for both **string‑type** and **byte‑stream‑type** N  
- Offers **Dry‑Run simulation**, **RSA enc/dec validation**, **Brent / ECM factoring**  
- Built‑in **Mask Editor (FrmMaskEditor)** supporting `*` variable position definition  
- Fully **bilingual interface** 
- All operations produce **color‑highlighted logs**

---

## 🧩 Example

| Type | Description | Example |
|:--|:--|:--|
| Original N | Original RSA modulus (green background marks original N) | `duDe1rHc22OLeI9tElSwEIhKIx9X/VOEDWC2jGDo1iUitTWFaROy1KHuYRi/ruz19BZIUUE5xIUeL7tzVmCasufYKwzj2MWTzpZdHjDMoU4ow5o5oa2j2soUnSQn6K5VSfhGxxnhmtcwoBp3qiaM3p085zkdyye46Cx4jaS+nSk` |
| N1 | Half‑nibble tweak (minimal difference) | `duDe1rHc22OLeI9tElSw**B**IhKIx9X/VOEDWC2jGDo1iUitTWFaROy1KHuYRi/ruz19BZIUUE5xIUeL7tzVmCasufYKwzj2MWTzpZdHjDMoU4ow5o5oa2j2soUnSQn6K5VSfhGxxnhmtcwoBp3qiaM3p085zkdyye46Cx4jaS+nSk` |
| N2 | Fixed CRC32 variant (CRC32 preserved) | `duDe1**I**Hc22OLeI9tElSwEIhKIx9X/VOEDWC2jGDo1iUitTWFaROy1**H**HuY**R**S/ruz19BZIUUE5xIUeL7tzVmCasuf**Q**Kwzj2MWTzpZdHjDMoU4ow5o5oa2j2soUnSQ**y**6K5VSfhGxxnhmtc**w**kBp3q**N**aM3p085zkdyye4**J**C**x**fjaS+nSk` |
| N3 | Another illustrative variant | `**keygenGeneratedBySomeCoolGuy**/VOEDWC2jGDo1iUit**M**WFaROy1**K**HkYRi/ru**0**19BZIUU**2**5xIUeL7t**D**cmCasu**1**YK**y**zj2MWTzpZdHjDM**P**U4**o**a5o5oa2j2soUnS**1**n6K5VSf**9**Gxxnhj**t**cwoBp3q**i**xM3p085zkdyye46Cx4jaS+nSk` |

> In the web version, differences are shown in **bold red**, and the original N line is highlighted in green (#ecfdf5).

---

## 🚀 Quick Start

1. **Paste [P]** to import N; the format is auto‑detected (Hex/Decimal/Base64).  
2. If detection is incorrect, click **isHEX / isDecimal / isBase64** to correct it (no conversion).  
3. Need conversion? Just change the format dropdown (e.g., Hex → Base64).  
4. Choose your run mode:  
   - **Byte‑Tweak Mode**: set “Try replace at” and “Direction”; optionally enable “Half‑nibble” and “HEX input is a string”.  
   - **Fixed CRC32 Mode**: click **Mask** to define `*` (variable) / frozen bits, set charset and Unicode mode.  
5. Click **Start**; candidates will appear in the log window with diffs highlighted in red.

---

## ⚙️ Options

### Top Input Area

- **TextN**: Holds the text value of **N**; supports **Hex / Decimal / Base64** formats.  
- **ComboFormat**: Switches the display and conversion format.  
- **Paste [P]**: Cleans whitespace and auto-detects the format; Base64 input will be auto-padded with `=`.  
- **Reverse [R]**: Reverses the entire sequence by byte order.  
- **isHEX / isDecimal / isBase64 / isHex(lower)**: Changes only the dropdown label without modifying the actual data.  

### Middle Options · Byte-Tweak Mode

- **Replace Position (Dec)** *(1-based)* — Index where substitution starts.  
- **Direction** — Choose **Current Only / Auto Left / Auto Right**.  
- **Nibble-Only Replace** — Perform a half-byte (4-bit) tweak; works for both **string-type** and **byte-stream-type** N.  
- **HEX as String** — Treat HEX input as plain text rather than raw bytes.  

### Middle Options · Fixed CRC32 Mode

- **Mask** — `*` marks a variable bit/character; others remain frozen.  
- **Allowed Chars** — Defines the substitution charset (Hex / Dec / Base64 / Custom).  
- **Unicode String** — Switch between **ASCII** and **UTF-16LE** mode.  
- **Editable Bytes (Collision Space)** — Adjust the number of editable positions.  

### General Options

- **Dry Run** — Log operations only; no real computation or factoring.  
- **Random Enc/Dec Test** — Perform a random RSA encrypt/decrypt validation using N/D.  
- **Brent Iterations** — Maximum iterations for the **Rho-Brent** factoring method.  
- **Trial Range** — Enable basic trial division by small primes.  
- **ECM Curves** — Number of small elliptic curves to try during ECM.  
- **Min Results** — Minimum number of results to generate.  
- **Log Results** — Write candidate results to the log window.  
- **Threads** — Sets parallel thread count.  
- **Prime N** — Allow N to be a prime number.  

---

## 📦 Dependencies and Redistribution

This program depends on the following runtime DLLs:

- **libecm-1.dll** – Provides ECM (Elliptic Curve Method) factoring support  
- **libgmp-10.dll** – GNU Multiple Precision Arithmetic Library (GMP)  
- **libwinpthread-1.dll** – Windows POSIX threading support  

All the above libraries are distributed under **LGPL/GPL-compatible licenses** and may be freely redistributed under compliant conditions.  
RSA PATCH TOOLKIT links to them dynamically without modifying their source code.

---

## ⚖️ License
This project is licensed under the Apache License 2.0.

© 2025 RSA PATCH TOOLKIT. All rights reserved.  
Code and documentation are open for non‑commercial study and modification, with attribution.
