#!/usr/bin/env python3
"""
browser_cookies.py - Extract and decrypt cookies from Firefox/Zen or Chrome/Chromium.

Usage: python browser_cookies.py <cookiefile> [host_filter]
  cookiefile   : path to cookies.sqlite (Firefox/Zen) or Cookies (Chrome)
  host_filter  : optional partial host to filter by, e.g. "google.com"

Outputs JSON array of { host, name, value } to stdout.
Diagnostic prints go to stderr so they don't pollute the JSON output.
"""

import json
import os
import shutil
import sqlite3
import sys
import tempfile
import ctypes
import struct
from pathlib import Path


def eprint(*args, **kwargs):
    """Print to stderr so it doesn't contaminate the JSON stdout output."""
    print(*args, file=sys.stderr, **kwargs)


# ---------------------------------------------------------------------------
# Firefox / Zen  (cookies.sqlite  +  key4.db  for decryption)
# ---------------------------------------------------------------------------

def find_key4db(cookies_path: str) -> str | None:
    """Look for key4.db in the same profile directory as cookies.sqlite."""
    profile_dir = os.path.dirname(cookies_path)
    candidate = os.path.join(profile_dir, "key4.db")
    if os.path.exists(candidate):
        return candidate
    eprint("key4.db not found next to cookies.sqlite — will fall back to plaintext values only.")
    return None


def decrypt_firefox_3des(data: bytes, key: bytes) -> bytes:
    """Decrypt 3DES-CBC with the given key (24 bytes) and IV embedded in data."""
    from Crypto.Cipher import DES3
    # Firefox stores IV as first 8 bytes of the ciphertext block in some encodings,
    # but when coming through NSS ASN.1 the IV is separate. We receive raw
    # (iv, ciphertext) already split by the caller.
    iv = data[:8]
    ciphertext = data[8:]
    cipher = DES3.new(key, DES3.MODE_CBC, iv)
    plaintext = cipher.decrypt(ciphertext)
    # PKCS7 unpad
    pad = plaintext[-1]
    return plaintext[:-pad]


def decode_ber_length(data: bytes, offset: int):
    """Minimal BER length decoder. Returns (length, new_offset)."""
    b = data[offset]
    offset += 1
    if b & 0x80 == 0:
        return b, offset
    num_bytes = b & 0x7F
    length = 0
    for _ in range(num_bytes):
        length = (length << 8) | data[offset]
        offset += 1
    return length, offset


def parse_firefox_asn1_pbe(data: bytes):
    """
    Very minimal ASN.1 parser for Firefox PBE structures.
    Returns (salt, iteration_count, iv, ciphertext) or raises.
    """
    # We use pyasn1 if available, otherwise fall back to a manual parse.
    try:
        from pyasn1.codec.der import decoder as der_decoder
        from pyasn1.type import univ
        seq, _ = der_decoder.decode(data)
        # Structure: SEQUENCE { SEQUENCE { OID, SEQUENCE { salt, iter } }, ciphertext }
        salt = bytes(seq[0][1][0])
        iterations = int(seq[0][1][1])
        iv = bytes(seq[0][1][2]) if len(seq[0][1]) > 2 else b'\x00' * 8
        ciphertext = bytes(seq[1])
        return salt, iterations, iv, ciphertext
    except Exception:
        pass

    # Manual fallback: walk the raw bytes looking for OCTET STRINGs
    # This is fragile but works for the common Firefox key4.db layout.
    octet_strings = []
    integers = []
    i = 0
    while i < len(data):
        tag = data[i]
        i += 1
        if i >= len(data):
            break
        length, i = decode_ber_length(data, i)
        value = data[i:i + length]
        i += length
        if tag == 0x04:  # OCTET STRING
            octet_strings.append(value)
        elif tag == 0x02:  # INTEGER
            val = int.from_bytes(value, 'big')
            integers.append(val)

    if len(octet_strings) >= 3:
        salt, iv, ciphertext = octet_strings[0], octet_strings[1], octet_strings[2]
        iterations = integers[0] if integers else 1
        return salt, iterations, iv, ciphertext
    elif len(octet_strings) == 2:
        salt, ciphertext = octet_strings
        iv = b'\x00' * 8
        iterations = integers[0] if integers else 1
        return salt, iterations, iv, ciphertext

    raise ValueError("Could not parse ASN.1 PBE structure")


def get_firefox_master_key(key4db_path: str, master_password: str = "") -> bytes | None:
    """
    Extract the master encryption key from Firefox's key4.db.
    Returns the 24-byte 3DES key used to decrypt cookie values, or None on failure.
    """
    try:
        from Crypto.Cipher import DES3
        from Crypto.Protocol.KDF import PBKDF2
        from Crypto.Hash import SHA1, HMAC
    except ImportError:
        eprint("pycryptodome not installed — cannot decrypt Firefox cookies.")
        eprint("Run: pip install pycryptodome")
        return None

    try:
        tmp = tempfile.NamedTemporaryFile(delete=False, suffix=".db")
        tmp.close()
        shutil.copyfile(key4db_path, tmp.name)

        conn = sqlite3.connect(tmp.name)
        cur = conn.cursor()
        cur.execute("SELECT name FROM sqlite_master WHERE type='table';")
        eprint("Tables:", cur.fetchall())
        cur.execute("PRAGMA table_info(moz_cookies);")
        eprint("moz_cookies columns:", cur.fetchall())

        # -- Step 1: derive the password-check key from the metadata row --
        # Try both casings
        try:
            cur.execute("SELECT item1, item2 FROM metaData WHERE id = 'password';")
        except sqlite3.OperationalError:
            cur.execute("SELECT item1, item2 FROM metadata WHERE id = 'password';")
        row = cur.fetchone()
        if not row:
            eprint("No password row in key4.db metadata.")
            conn.close()
            return None

        global_salt = row[0]  # stored as BLOB
        password_check_asn1 = row[1]  # stored as BLOB

        if isinstance(global_salt, str):
            global_salt = global_salt.encode('latin-1')
        if isinstance(password_check_asn1, str):
            password_check_asn1 = password_check_asn1.encode('latin-1')

        salt, iterations, iv, ciphertext = parse_firefox_asn1_pbe(password_check_asn1)

        hp = SHA1.new(global_salt + master_password.encode('utf-8')).digest()
        derived = PBKDF2(hp, salt, dkLen=32, count=iterations,
                         prf=lambda p, s: HMAC.new(p, s, SHA1).digest())
        key = derived[:24]
        iv2 = derived[32:] if len(derived) > 32 else iv

        # Verify master password by decrypting the check value
        try:
            cipher = DES3.new(key, DES3.MODE_CBC, iv2[:8])
            decrypted = cipher.decrypt(ciphertext)
            # Firefox stores "password-check\x02\x02" as the check plaintext
            if b'password-check' not in decrypted:
                eprint("Master password incorrect or key derivation mismatch.")
                conn.close()
                return None
        except Exception as e:
            eprint(f"Password check decryption failed: {e}")
            conn.close()
            return None

        # -- Step 2: extract the actual encryption key from nssPrivate --
        try:
            cur.execute("SELECT a11, a102 FROM nssPrivate;")
        except sqlite3.OperationalError:
            cur.execute("SELECT a11, a102 FROM nssprivate;")
        rows = cur.fetchall()
        conn.close()

        for a11, _ in rows:
            if isinstance(a11, str):
                a11 = a11.encode('latin-1')
            try:
                salt2, iter2, iv3, ct2 = parse_firefox_asn1_pbe(a11)
                hp2 = SHA1.new(global_salt + master_password.encode('utf-8')).digest()
                derived2 = PBKDF2(hp2, salt2, dkLen=32, count=iter2,
                                  prf=lambda p, s: HMAC.new(p, s, SHA1).digest())
                key2 = derived2[:24]
                iv4 = derived2[32:40] if len(derived2) > 32 else iv3[:8]
                cipher2 = DES3.new(key2, DES3.MODE_CBC, iv4)
                decrypted2 = cipher2.decrypt(ct2)
                # The actual key material starts at byte 4, length 24
                if len(decrypted2) >= 28:
                    return decrypted2[4:28]
            except Exception as e:
                eprint(f"nssPrivate row decryption error: {e}")
                continue

        eprint("Could not extract encryption key from nssPrivate.")
        return None

    except Exception as e:
        eprint(f"get_firefox_master_key error: {e}")
        return None
    finally:
        try:
            os.unlink(tmp.name)
        except Exception:
            pass


def decrypt_firefox_cookie_value(encrypted_blob: bytes, master_key: bytes) -> str | None:
    """
    Decrypt a single Firefox cookie value blob using the master key.
    Cookie values in moz_cookies are ASN.1-encoded when encrypted.
    """
    if not encrypted_blob or not master_key:
        return None
    try:
        from Crypto.Cipher import DES3
        salt, iterations, iv, ciphertext = parse_firefox_asn1_pbe(encrypted_blob)
        # For cookie values Firefox uses the master key directly with the embedded IV
        cipher = DES3.new(master_key, DES3.MODE_CBC, iv[:8])
        decrypted = cipher.decrypt(ciphertext)
        # PKCS7 unpad
        pad = decrypted[-1]
        if pad < 1 or pad > 8:
            pad = 0
        return decrypted[:-pad].decode('utf-8', errors='replace').strip('\x00')
    except Exception as e:
        eprint(f"Cookie decryption error: {e}")
        return None


def get_firefox_cookies(cookiesfile: str, url: str | None = None) -> list[dict]:
    """
    Read cookies from a Firefox/Zen cookies.sqlite file.
    Attempts decryption via key4.db in the same profile directory.
    Falls back to plaintext values if decryption is unavailable.
    """
    eprint(f"Reading Firefox/Zen cookies from: {cookiesfile}")

    # Copy to temp to avoid lock issues
    tmp = tempfile.NamedTemporaryFile(delete=False, suffix=".sqlite")
    tmp.close()
    shutil.copyfile(cookiesfile, tmp.name)

    master_key = None
    key4db = find_key4db(cookiesfile)
    if key4db:
        eprint(f"Found key4.db at: {key4db}")
        master_key = get_firefox_master_key(key4db)
        if master_key:
            eprint(f"Master key extracted successfully ({len(master_key)} bytes).")
        else:
            eprint("Master key extraction failed — cookie values may be empty for encrypted cookies.")

    conn = sqlite3.connect(tmp.name, timeout=3)
    cur = conn.cursor()

    #Force WAL checkpoint
    cur.execute("PRAGMA wal_checkpoint(FULL);")
    eprint("WAL checkpoint result:", cur.fetchall())

    cur.execute("SELECT name FROM sqlite_master WHERE type='table';")
    eprint("Tables:", cur.fetchall())
    cur.execute("PRAGMA table_info(moz_cookies);")
    eprint("moz_cookies columns:", cur.fetchall())
    # moz_cookies schema: host, name, value, encType, encryptedValue
    # encType 1 = encrypted, encType 0 = plaintext
    try:
        cur.execute("SELECT host, name, value, encType, encryptedValue FROM moz_cookies" +
                    (f" WHERE host LIKE '%{url}%'" if url else ""))
        rows = cur.fetchall()
    except sqlite3.OperationalError:
        # Older schema without encType/encryptedValue
        eprint("Old schema detected, reading plaintext values only.")
        cur.execute("SELECT host, name, value FROM moz_cookies" +
                    (f" WHERE host LIKE '%{url}%'" if url else ""))
        rows = [(h, n, v, 0, None) for h, n, v in cur.fetchall()]

    cur.execute("SELECT host, name, value FROM moz_cookies WHERE name = 'SIDCC' AND host LIKE '%google.com%';")
    eprint("SIDCC values:", cur.fetchall())
    conn.close()
    os.unlink(tmp.name)

    cookies = []
    decrypted_count = 0
    plaintext_count = 0
    failed_count = 0

    for host, name, value, enc_type, encrypted_value in rows:
        if enc_type == 1 and encrypted_value:
            # Encrypted cookie
            if master_key:
                if isinstance(encrypted_value, str):
                    encrypted_value = encrypted_value.encode('latin-1')
                decrypted = decrypt_firefox_cookie_value(encrypted_value, master_key)
                if decrypted:
                    cookies.append({'host': host, 'name': name, 'value': decrypted})
                    decrypted_count += 1
                else:
                    eprint(f"  Failed to decrypt: {name} @ {host}")
                    cookies.append({'host': host, 'name': name, 'value': ''})
                    failed_count += 1
            else:
                eprint(f"  No master key, skipping encrypted cookie: {name} @ {host}")
                cookies.append({'host': host, 'name': name, 'value': ''})
                failed_count += 1
        else:
            cookies.append({'host': host, 'name': name, 'value': value or ''})
            plaintext_count += 1

    eprint(f"Cookies: {plaintext_count} plaintext, {decrypted_count} decrypted, {failed_count} failed.")
    return cookies


# ---------------------------------------------------------------------------
# Chrome / Chromium  (Cookies  - SQLite with DPAPI/AES encryption)
# ---------------------------------------------------------------------------

def get_chrome_local_state_key(cookies_path: str) -> bytes | None:
    """Extract the AES key from Chrome's Local State file (v10+ cookies on Windows)."""
    import base64
    local_state_path = os.path.join(os.path.dirname(cookies_path), '..', 'Local State')
    local_state_path = os.path.normpath(local_state_path)
    if not os.path.exists(local_state_path):
        eprint(f"Chrome Local State not found at {local_state_path}")
        return None
    try:
        with open(local_state_path, 'r', encoding='utf-8') as f:
            local_state = json.load(f)
        encrypted_key = base64.b64decode(local_state['os_crypt']['encrypted_key'])
        # Strip DPAPI prefix "DPAPI"
        encrypted_key = encrypted_key[5:]
        if sys.platform == 'win32':
            import ctypes
            import ctypes.wintypes
            class DATA_BLOB(ctypes.Structure):
                _fields_ = [("cbData", ctypes.wintypes.DWORD),
                             ("pbData", ctypes.POINTER(ctypes.c_char))]
            p = ctypes.create_string_buffer(encrypted_key, len(encrypted_key))
            blobin = DATA_BLOB(ctypes.sizeof(p), p)
            blobout = DATA_BLOB()
            retval = ctypes.windll.crypt32.CryptUnprotectData(
                ctypes.byref(blobin), None, None, None, None, 0, ctypes.byref(blobout))
            if not retval:
                eprint("DPAPI decryption of Chrome key failed.")
                return None
            key = ctypes.string_at(blobout.pbData, blobout.cbData)
            ctypes.windll.kernel32.LocalFree(blobout.pbData)
            return key
    except Exception as e:
        eprint(f"Chrome local state key error: {e}")
    return None


def chrome_decrypt_v10(encrypted_value: bytes, aes_key: bytes) -> str:
    """Decrypt a Chrome v10 AES-GCM encrypted cookie value."""
    from Crypto.Cipher import AES
    # Format: b'v10' + 12-byte nonce + ciphertext + 16-byte tag
    nonce = encrypted_value[3:15]
    ciphertext = encrypted_value[15:-16]
    tag = encrypted_value[-16:]
    cipher = AES.new(aes_key, AES.MODE_GCM, nonce=nonce)
    return cipher.decrypt_and_verify(ciphertext, tag).decode('utf-8', errors='replace')


def chrome_decrypt_dpapi(encrypted_value: bytes) -> str:
    """Decrypt a Chrome DPAPI-encrypted cookie value (Windows, older format)."""
    if sys.platform != 'win32':
        return ''
    import ctypes
    import ctypes.wintypes
    class DATA_BLOB(ctypes.Structure):
        _fields_ = [("cbData", ctypes.wintypes.DWORD),
                    ("pbData", ctypes.POINTER(ctypes.c_char))]
    p = ctypes.create_string_buffer(encrypted_value, len(encrypted_value))
    blobin = DATA_BLOB(ctypes.sizeof(p), p)
    blobout = DATA_BLOB()
    retval = ctypes.windll.crypt32.CryptUnprotectData(
        ctypes.byref(blobin), None, None, None, None, 0, ctypes.byref(blobout))
    if not retval:
        return ''
    result = ctypes.string_at(blobout.pbData, blobout.cbData).decode('utf-8', errors='replace')
    ctypes.windll.kernel32.LocalFree(blobout.pbData)
    return result


def get_chrome_cookies(cookiesfile: str, url: str | None = None) -> list[dict]:
    """Read and decrypt cookies from a Chrome/Chromium Cookies SQLite file."""
    eprint(f"Reading Chrome cookies from: {cookiesfile}")

    aes_key = get_chrome_local_state_key(cookiesfile)
    if aes_key:
        eprint(f"Chrome AES key extracted ({len(aes_key)} bytes).")
    else:
        eprint("No AES key — will attempt DPAPI decryption only.")

    tmp = tempfile.NamedTemporaryFile(delete=False, suffix=".db")
    tmp.close()
    shutil.copyfile(cookiesfile, tmp.name)

    conn = sqlite3.connect(tmp.name, timeout=3)
    cur = conn.cursor()
    query = 'SELECT host_key, name, value, encrypted_value FROM cookies'
    if url:
        query += f" WHERE host_key LIKE '%{url}%'"
    cur.execute(query)
    rows = cur.fetchall()
    conn.close()
    os.unlink(tmp.name)

    cookies = []
    for host_key, name, value, encrypted_value in rows:
        if value:
            cookies.append({'host': host_key, 'name': name, 'value': value})
            continue
        if not encrypted_value:
            cookies.append({'host': host_key, 'name': name, 'value': ''})
            continue
        try:
            if encrypted_value[:3] == b'v10' and aes_key:
                decrypted = chrome_decrypt_v10(encrypted_value, aes_key)
            else:
                decrypted = chrome_decrypt_dpapi(encrypted_value)
            cookies.append({'host': host_key, 'name': name, 'value': decrypted})
        except Exception as e:
            eprint(f"  Chrome decrypt error for {name} @ {host_key}: {e}")
            cookies.append({'host': host_key, 'name': name, 'value': ''})

    return cookies


# ---------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print(f"Usage: {os.path.basename(sys.argv[0])} <cookiefile> [host_filter]", file=sys.stderr)
        print("  cookiefile  : cookies.sqlite (Firefox/Zen) or Cookies (Chrome)", file=sys.stderr)
        print("  host_filter : optional partial host, e.g. google.com", file=sys.stderr)
        sys.exit(1)

    cookiefile = os.path.expanduser(sys.argv[1])
    url = sys.argv[2] if len(sys.argv) > 2 else None

    if not os.path.exists(cookiefile):
        eprint(f"File not found: {cookiefile}")
        sys.exit(1)

    if cookiefile.endswith('.sqlite'):
        cookies = get_firefox_cookies(cookiefile, url)
    else:
        cookies = get_chrome_cookies(cookiefile, url)


    # JSON output to stdout only — all diagnostics go to stderr
    print(json.dumps(cookies, indent=4, sort_keys=True))
